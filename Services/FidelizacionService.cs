using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ResumenPuntosDTO
    {
        public int IdCliente { get; set; }
        public int PuntosDisponibles { get; set; }
        public int PuntosHistoricosAcumulados { get; set; }
        public int PuntosHistoricosCanjeados { get; set; }
        public decimal EquivalenciaPesosDisponible => PuntosDisponibles * FidelizacionConfig.ValorPesoPorPunto;
    }

    public class PuntosMovimientoDTO
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int Puntos { get; set; }
        public decimal MontoBase { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public int? IdTve { get; set; }
    }

    public class FidelizacionService
    {
        // =========================================================================
        // 1. CONSULTAS
        // =========================================================================
        public ResumenPuntosDTO ObtenerResumenPuntos(int idCliente)
        {
            if (idCliente <= 0) return new ResumenPuntosDTO();

            using var db = new AppDbContext();
            var cuenta = db.ClienteCuentasPuntos.AsNoTracking().FirstOrDefault(c => c.IdCliente == idCliente);

            if (cuenta == null)
            {
                return new ResumenPuntosDTO
                {
                    IdCliente = idCliente,
                    PuntosDisponibles = 0,
                    PuntosHistoricosAcumulados = 0,
                    PuntosHistoricosCanjeados = 0
                };
            }

            return new ResumenPuntosDTO
            {
                IdCliente = idCliente,
                PuntosDisponibles = cuenta.PuntosDisponibles,
                PuntosHistoricosAcumulados = cuenta.PuntosAcumuladosHistorico,
                PuntosHistoricosCanjeados = cuenta.PuntosCanjeadosHistorico
            };
        }

        public List<PuntosMovimientoDTO> ObtenerHistorialMovimientos(int idCliente)
        {
            if (idCliente <= 0) return new List<PuntosMovimientoDTO>();

            using var db = new AppDbContext();
            return db.PuntosMovimientos
                     .AsNoTracking()
                     .Where(m => m.IdCliente == idCliente)
                     .OrderByDescending(m => m.Fecha)
                     .Select(m => new PuntosMovimientoDTO
                     {
                         Fecha = m.Fecha,
                         Tipo = m.Tipo,
                         Puntos = m.Puntos,
                         MontoBase = m.MontoBase,
                         Descripcion = m.Descripcion,
                         Usuario = m.Usuario,
                         IdTve = m.IdTve
                     })
                     .ToList();
        }

        public int CalcularPuntosPorMonto(decimal montoElegible)
        {
            if (montoElegible < FidelizacionConfig.MontoMinimoAcumulacion) return 0;
            return (int)Math.Floor(montoElegible / FidelizacionConfig.MontoBasePorPunto) * FidelizacionConfig.PuntosPorBloque;
        }

        // =========================================================================
        // 2. ACUMULACIÓN AUTOMÁTICA POR VENTA CONFIRMADA
        // =========================================================================
        public int ProcesarAcumulacionPorVenta(int idCliente, int idTve, decimal montoElegiblePagado, int folioDte, string usuarioCajero)
        {
            if (idCliente <= 0 || montoElegiblePagado < FidelizacionConfig.MontoMinimoAcumulacion) return 0;

            int puntosAGanar = CalcularPuntosPorMonto(montoElegiblePagado);
            if (puntosAGanar <= 0) return 0;

            using var db = new AppDbContext();
            using var tr = db.Database.BeginTransaction();
            try
            {
                // Validación de idempotencia: evita duplicar acumulación si se reintenta
                bool yaAcumulo = db.PuntosMovimientos.Any(m => m.IdTve == idTve && m.Tipo == "ACUMULACION");
                if (yaAcumulo)
                {
                    return 0;
                }

                var cuenta = db.ClienteCuentasPuntos.FirstOrDefault(c => c.IdCliente == idCliente);
                DateTime ahora = DateTime.Now;

                if (cuenta == null)
                {
                    cuenta = new ClienteCuentaPuntos
                    {
                        IdCliente = idCliente,
                        PuntosDisponibles = puntosAGanar,
                        PuntosAcumuladosHistorico = puntosAGanar,
                        PuntosCanjeadosHistorico = 0,
                        FechaUltimaActualizacion = ahora,
                        VersionFila = 1
                    };
                    db.ClienteCuentasPuntos.Add(cuenta);
                }
                else
                {
                    cuenta.PuntosDisponibles += puntosAGanar;
                    cuenta.PuntosAcumuladosHistorico += puntosAGanar;
                    cuenta.FechaUltimaActualizacion = ahora;
                    cuenta.VersionFila++;
                }

                var movimiento = new PuntosMovimiento
                {
                    IdCliente = idCliente,
                    IdTve = idTve,
                    Fecha = ahora,
                    Tipo = "ACUMULACION",
                    Puntos = puntosAGanar,
                    MontoBase = montoElegiblePagado,
                    Descripcion = $"Acumulación por compra DTE N° {folioDte}",
                    Usuario = usuarioCajero
                };

                db.PuntosMovimientos.Add(movimiento);
                db.SaveChanges();
                tr.Commit();

                return puntosAGanar;
            }
            catch
            {
                tr.Rollback();
                return 0;
            }
        }

        // =========================================================================
        // 3. CANJE / UTILIZACIÓN DE PUNTOS COMO DESCUENTO
        // =========================================================================
        public (bool Exito, string Mensaje) ProcesarCanjePorVenta(int idCliente, int idTve, int puntosACanjear, int folioDte, string usuarioCajero)
        {
            if (idCliente <= 0 || puntosACanjear <= 0) 
                return (false, "Datos de canje inválidos.");

            using var db = new AppDbContext();
            using var tr = db.Database.BeginTransaction();
            try
            {
                // Validación de idempotencia: no registrar dos veces el mismo canje para esta venta
                bool yaCanjeo = db.PuntosMovimientos.Any(m => m.IdTve == idTve && m.Tipo == "CANJE");
                if (yaCanjeo)
                {
                    return (true, "El canje ya fue procesado con anterioridad.");
                }

                var cuenta = db.ClienteCuentasPuntos.FirstOrDefault(c => c.IdCliente == idCliente);
                if (cuenta == null || cuenta.PuntosDisponibles < puntosACanjear)
                {
                    tr.Rollback();
                    return (false, "Saldo de puntos insuficiente.");
                }

                DateTime ahora = DateTime.Now;
                decimal montoDescuento = puntosACanjear * FidelizacionConfig.ValorPesoPorPunto;

                cuenta.PuntosDisponibles -= puntosACanjear;
                cuenta.PuntosCanjeadosHistorico += puntosACanjear;
                cuenta.FechaUltimaActualizacion = ahora;
                cuenta.VersionFila++;

                var movimiento = new PuntosMovimiento
                {
                    IdCliente = idCliente,
                    IdTve = idTve,
                    Fecha = ahora,
                    Tipo = "CANJE",
                    Puntos = -puntosACanjear,
                    MontoBase = montoDescuento,
                    Descripcion = $"Canje de {puntosACanjear:N0} pts (-${montoDescuento:N0}) en DTE N° {folioDte}",
                    Usuario = usuarioCajero
                };

                db.PuntosMovimientos.Add(movimiento);
                db.SaveChanges();
                tr.Commit();

                return (true, "Canje procesado exitosamente.");
            }
            catch (Exception ex)
            {
                tr.Rollback();
                return (false, $"Error al procesar canje: {ex.Message}");
            }
        }

        // =========================================================================
        // 4. REVERSA DE PUNTOS POR ANULACIÓN DE VENTA
        // =========================================================================
        public void ReversarPuntosPorAnulacion(int idTve, string usuarioCajero)
        {
            using var db = new AppDbContext();
            using var tr = db.Database.BeginTransaction();
            try
            {
                var movimientosVenta = db.PuntosMovimientos.Where(m => m.IdTve == idTve).ToList();
                if (movimientosVenta.Count == 0) return;

                DateTime ahora = DateTime.Now;

                foreach (var mov in movimientosVenta)
                {
                    var cuenta = db.ClienteCuentasPuntos.FirstOrDefault(c => c.IdCliente == mov.IdCliente);
                    if (cuenta == null) continue;

                    if (mov.Tipo == "ACUMULACION")
                    {
                        // Se retiran los puntos que se habían otorgado
                        cuenta.PuntosDisponibles = Math.Max(0, cuenta.PuntosDisponibles - mov.Puntos);
                        cuenta.PuntosAcumuladosHistorico = Math.Max(0, cuenta.PuntosAcumuladosHistorico - mov.Puntos);

                        db.PuntosMovimientos.Add(new PuntosMovimiento
                        {
                            IdCliente = mov.IdCliente,
                            IdTve = idTve,
                            Fecha = ahora,
                            Tipo = "REVERSA",
                            Puntos = -mov.Puntos,
                            MontoBase = mov.MontoBase,
                            Descripcion = "Reversa de acumulación por anulación de Ticket/DTE",
                            Usuario = usuarioCajero
                        });
                    }
                    else if (mov.Tipo == "CANJE")
                    {
                        // Se restituyen los puntos que el cliente había utilizado
                        int puntosDevueltos = Math.Abs(mov.Puntos);
                        cuenta.PuntosDisponibles += puntosDevueltos;
                        cuenta.PuntosCanjeadosHistorico = Math.Max(0, cuenta.PuntosCanjeadosHistorico - puntosDevueltos);

                        db.PuntosMovimientos.Add(new PuntosMovimiento
                        {
                            IdCliente = mov.IdCliente,
                            IdTve = idTve,
                            Fecha = ahora,
                            Tipo = "REVERSA",
                            Puntos = puntosDevueltos,
                            MontoBase = mov.MontoBase,
                            Descripcion = "Restitución de puntos canjeados por anulación de Ticket/DTE",
                            Usuario = usuarioCajero
                        });
                    }

                    cuenta.FechaUltimaActualizacion = ahora;
                    cuenta.VersionFila++;
                }

                db.SaveChanges();
                tr.Commit();
            }
            catch
            {
                tr.Rollback();
            }
        }
    }
}