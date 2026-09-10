using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ValidacionCreditoResult
    {
        public bool EsValido { get; set; }
        public string MensajeError { get; set; } = string.Empty;
    }

    public class ResumenCarteraCxCDTO
    {
        public decimal TotalDeuda { get; set; }
        public decimal DeudaVencida { get; set; }
        public int DocsPendientes { get; set; }
        public int ClientesMora { get; set; }
        public List<ItemCuentaPorCobrarDTO> Items { get; set; } = new List<ItemCuentaPorCobrarDTO>();
    }

    public class ItemCuentaPorCobrarDTO
    {
        public int CxCID { get; set; }
        public int IdCliente { get; set; }
        public int TipoDTE { get; set; }
        public int FolioDoc { get; set; }
        public string DocumentoNombre { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasMora { get; set; }
        public string MoraTexto { get; set; } = string.Empty;
        public decimal MontoOriginal { get; set; }
        public decimal MontoAbonado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public static class CreditoService
    {
        public static ResumenCarteraCxCDTO ObtenerCartera(DateTime fDesde, DateTime fHasta, string filtro = "", string estado = "Todos")
        {
            ActualizarEstadosMorosidadDiario();

            using var db = new AppDbContext();
            DateTime hoy = DateTime.Today;

            var query = db.CuentasPorCobrar
                .AsNoTracking()
                .Where(c => c.FechaEmision >= fDesde && c.FechaEmision <= fHasta)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string q = filtro.Trim().ToLower();
                query = query.Where(c => (c.Cliente != null && (c.Cliente.RazonSocial.ToLower().Contains(q) || c.Cliente.Rut.Contains(q))) || c.FolioDoc.ToString().Contains(q));
            }

            if (estado != "Todos")
            {
                query = query.Where(c => c.Estado == estado);
            }

            var cxcCargadas = query.OrderBy(c => c.FechaVencimiento).ToList();
            var idsClientes = cxcCargadas.Select(c => c.IdCliente).Distinct().ToList();
            var clientesDict = db.Clientes.AsNoTracking().Where(c => idsClientes.Contains(c.IdCliente)).ToDictionary(c => c.IdCliente, c => c);

            var listaItems = new List<ItemCuentaPorCobrarDTO>();

            foreach (var c in cxcCargadas)
            {
                clientesDict.TryGetValue(c.IdCliente, out var cli);
                string nomCli = cli?.RazonSocial ?? "Cliente General";
                string rutCli = cli != null ? Helpers.RutHelper.Formatear(cli.Rut) : "--";

                int diasMora = (hoy > c.FechaVencimiento.Date && c.SaldoPendiente > 0) ? (hoy - c.FechaVencimiento.Date).Days : 0;
                string strMora = diasMora > 0 ? $"🔴 {diasMora} d" : "🟢 Al día";
                string nomDoc = c.TipoDTE == 33 ? $"Factura #{c.FolioDoc}" : $"Boleta #{c.FolioDoc}";

                listaItems.Add(new ItemCuentaPorCobrarDTO
                {
                    CxCID = c.CxCID,
                    IdCliente = c.IdCliente,
                    TipoDTE = c.TipoDTE,
                    FolioDoc = c.FolioDoc,
                    DocumentoNombre = nomDoc,
                    ClienteNombre = nomCli,
                    Rut = rutCli,
                    FechaEmision = c.FechaEmision,
                    FechaVencimiento = c.FechaVencimiento,
                    DiasMora = diasMora,
                    MoraTexto = strMora,
                    MontoOriginal = c.MontoOriginal,
                    MontoAbonado = c.MontoAbonado,
                    SaldoPendiente = c.SaldoPendiente,
                    Estado = c.Estado
                });
            }

            decimal totalDeuda = db.CuentasPorCobrar.Where(c => c.Estado != "PAGADA" && c.Estado != "ANULADA").Sum(c => c.SaldoPendiente);
            decimal deudaVencida = db.CuentasPorCobrar.Where(c => c.FechaVencimiento < hoy && c.SaldoPendiente > 0).Sum(c => c.SaldoPendiente);
            int docsPendientes = db.CuentasPorCobrar.Count(c => c.Estado != "PAGADA" && c.Estado != "ANULADA");
            int clientesMora = db.Clientes.Count(c => c.EstadoCrediticio == "MOROSO" || c.EstadoCrediticio == "BLOQUEADO");

            return new ResumenCarteraCxCDTO
            {
                TotalDeuda = totalDeuda,
                DeudaVencida = deudaVencida,
                DocsPendientes = docsPendientes,
                ClientesMora = clientesMora,
                Items = listaItems
            };
        }

        public static List<PagoCliente> ObtenerHistorialPagosRecientes(int limite = 50)
        {
            using var db = new AppDbContext();
            return db.PagosClientes.AsNoTracking().OrderByDescending(p => p.FechaPago).Take(limite).ToList();
        }

        public static (Cliente? Cliente, decimal DeudaVencida, int CantidadVencidas) ObtenerDetalleCrediticioCliente(int idCliente)
        {
            using var db = new AppDbContext();
            var cliente = db.Clientes.AsNoTracking().FirstOrDefault(c => c.IdCliente == idCliente);
            if (cliente == null) return (null, 0, 0);

            DateTime hoy = DateTime.Today;
            var facturasVencidas = db.CuentasPorCobrar
                .AsNoTracking()
                .Where(c => c.IdCliente == idCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA") && c.FechaVencimiento < hoy)
                .ToList();

            decimal deudaVencidaTotal = facturasVencidas.Sum(f => f.SaldoPendiente);
            return (cliente, deudaVencidaTotal, facturasVencidas.Count);
        }

        public static void CambiarEstadoCrediticio(int idCliente, string nuevoEstado, string motivo, string usuarioResponsable)
        {
            using var db = new AppDbContext();
            var cliente = db.Clientes.Find(idCliente);
            if (cliente == null) return;

            string estadoAnt = cliente.EstadoCrediticio ?? "ACTIVO";
            cliente.EstadoCrediticio = nuevoEstado;

            db.HistorialCondicionesCredito.Add(new HistorialCondicionesCredito
            {
                IdCliente = cliente.IdCliente,
                DiasCreditoAnterior = cliente.DiasCreditoHabiles,
                DiasCreditoNuevo = cliente.DiasCreditoHabiles,
                CupoAnterior = cliente.CupoCredito,
                CupoNuevo = cliente.CupoCredito,
                EstadoAnterior = estadoAnt,
                EstadoNuevo = nuevoEstado,
                Motivo = motivo,
                FechaCambio = DateTime.Now,
                UsuarioResponsable = usuarioResponsable
            });

            db.SaveChanges();
        }

        public static DateTime CalcularFechaVencimientoHabil(DateTime fechaEmision, int diasHabiles, HashSet<DateTime> feriados)
        {
            if (diasHabiles <= 0) return fechaEmision.Date;

            DateTime fechaActual = fechaEmision.Date;
            int diasContados = 0;

            while (diasContados < diasHabiles)
            {
                fechaActual = fechaActual.AddDays(1);

                if (fechaActual.DayOfWeek == DayOfWeek.Saturday || fechaActual.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                if (feriados.Contains(fechaActual.Date))
                    continue;

                diasContados++;
            }

            return fechaActual;
        }

        public static ValidacionCreditoResult ValidarVentaCredito(Cliente cliente, decimal totalVenta, AppDbContext db)
        {
            if (cliente == null || !cliente.Estado)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente se encuentra inactivo o no está registrado." };

            int diasEfectivos = cliente.DiasCreditoHabiles > 0 ? cliente.DiasCreditoHabiles : cliente.DiasCredito;

            if (diasEfectivos <= 0 && !cliente.PermiteCredito)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente no tiene habilitada la línea de crédito comercial ni días asignados." };

            if (cliente.ModalidadPago == "SOLO_CONTADO" && diasEfectivos <= 0)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente está configurado exclusivamente para compras al contado." };

            if (cliente.EstadoCrediticio == "BLOQUEADO" || cliente.EstadoCrediticio == "SUSPENDIDO")
                return new ValidacionCreditoResult { EsValido = false, MensajeError = $"Crédito denegado. El cliente presenta estado crediticio: '{cliente.EstadoCrediticio}'." };

            DateTime hoy = DateTime.Today;
            bool tieneMora = db.CuentasPorCobrar
                .Any(c => c.IdCliente == cliente.IdCliente &&
                        (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA") &&
                        c.FechaVencimiento < hoy);

            if (tieneMora)
            {
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente mantiene documentos vencidos pendientes de pago. Debe regularizar su deuda para continuar facturando a crédito." };
            }

            decimal deudaActual = db.CuentasPorCobrar
                .Where(c => c.IdCliente == cliente.IdCliente &&
                            (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA"))
                .Sum(c => c.SaldoPendiente);

            if (cliente.CupoCredito > 0 && (deudaActual + totalVenta) > cliente.CupoCredito)
            {
                decimal cupoDisponible = Math.Max(0, cliente.CupoCredito - deudaActual);
                return new ValidacionCreditoResult
                {
                    EsValido = false,
                    MensajeError = $"La venta excede el cupo disponible del cliente.\n\nCupo Total: ${cliente.CupoCredito:N0}\nDeuda Actual: ${deudaActual:N0}\nCupo Disponible: ${cupoDisponible:N0}\nMonto Venta: ${totalVenta:N0}"
                };
            }

            return new ValidacionCreditoResult { EsValido = true };
        }

        // --- SOBRECARGA DESACOPLADA PARA FORMCAJA ---
        public static ValidacionCreditoResult ValidarVentaCreditoPorRutOId(int idCliente, string rut, decimal totalVenta)
        {
            using var db = new AppDbContext();
            string rutLimpio = Helpers.RutHelper.Limpiar(rut ?? "");
            var cliente = db.Clientes.FirstOrDefault(c => (idCliente > 0 && c.IdCliente == idCliente) || (c.Rut == rutLimpio || c.Rut == rut));

            if (cliente == null)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El ticket no tiene un cliente registrado para otorgar crédito comercial." };

            return ValidarVentaCredito(cliente, totalVenta, db);
        }

        public static void RegistrarFacturaCredito(int ventaId, int folioFactura, DateTime fechaEmision, Cliente cliente, decimal totalFactura, string usuarioEmisor, AppDbContext db)
        {
            var feriados = db.Feriados.Select(f => f.Fecha.Date).ToHashSet();
            int diasHabiles = cliente.DiasCreditoHabiles > 0 ? cliente.DiasCreditoHabiles : cliente.DiasCredito;

            DateTime fechaVenc = CalcularFechaVencimientoHabil(fechaEmision, diasHabiles, feriados);

            var cxc = new CuentaPorCobrar
            {
                VentaID = ventaId,
                IdCliente = cliente.IdCliente,
                TipoDTE = 33,
                FolioDoc = folioFactura,
                FechaEmision = fechaEmision,
                DiasCreditoHabiles = diasHabiles,
                FechaVencimiento = fechaVenc,
                MontoOriginal = totalFactura,
                MontoAbonado = 0.00m,
                SaldoPendiente = totalFactura,
                Estado = "PENDIENTE",
                UsuarioEmisor = string.IsNullOrWhiteSpace(usuarioEmisor) ? "SISTEMA" : usuarioEmisor,
                FechaRegistro = DateTime.Now
            };

            db.CuentasPorCobrar.Add(cxc);
            cliente.SaldoUtilizado += totalFactura;
            db.SaveChanges();
        }

        // --- SOBRECARGA DESACOPLADA PARA FORMCAJA ---
        public static void RegistrarFacturaCreditoDirecto(int ventaId, int folioFactura, DateTime fechaEmision, int idCliente, string rut, decimal totalFactura, string usuarioEmisor)
        {
            using var db = new AppDbContext();
            string rutLimpio = Helpers.RutHelper.Limpiar(rut ?? "");
            var cliente = db.Clientes.FirstOrDefault(c => (idCliente > 0 && c.IdCliente == idCliente) || (c.Rut == rutLimpio || c.Rut == rut));
            if (cliente == null) return;

            RegistrarFacturaCredito(ventaId, folioFactura, fechaEmision, cliente, totalFactura, usuarioEmisor, db);
        }

        public static void ProcesarPagoCliente(int idCliente, decimal montoPago, string medioPago, string nroComprobante, string observaciones, string usuarioCobrador, List<int>? cxcIdsSeleccionadas, AppDbContext? dbExistente = null)
        {
            if (montoPago <= 0) throw new ArgumentException("El monto a abonar debe ser superior a cero.");

            bool manejaContexto = (dbExistente == null);
            var db = dbExistente ?? new AppDbContext();

            using var trans = db.Database.BeginTransaction();
            try
            {
                var pago = new PagoCliente
                {
                    IdCliente = idCliente,
                    FechaPago = DateTime.Now,
                    MontoTotalPago = montoPago,
                    MedioPago = medioPago,
                    NroComprobante = nroComprobante,
                    Observaciones = observaciones,
                    UsuarioCobrador = string.IsNullOrWhiteSpace(usuarioCobrador) ? "SISTEMA" : usuarioCobrador
                };
                db.PagosClientes.Add(pago);
                db.SaveChanges();

                var query = db.CuentasPorCobrar
                    .Where(c => c.IdCliente == idCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA"));

                if (cxcIdsSeleccionadas != null && cxcIdsSeleccionadas.Count > 0)
                {
                    query = query.Where(c => cxcIdsSeleccionadas.Contains(c.CxCID));
                }

                var facturasPendientes = query.OrderBy(c => c.FechaVencimiento).ToList();
                decimal remanente = montoPago;

                foreach (var fac in facturasPendientes)
                {
                    if (remanente <= 0) break;

                    decimal saldoActualFac = fac.SaldoPendiente;
                    decimal montoAImputar = Math.Min(remanente, saldoActualFac);

                    fac.MontoAbonado += montoAImputar;
                    fac.SaldoPendiente = fac.MontoOriginal - fac.MontoAbonado;
                    fac.Estado = (fac.SaldoPendiente <= 0) ? "PAGADA" : "PARCIAL";

                    db.PagosDetalleFacturas.Add(new PagoDetalleFactura
                    {
                        PagoID = pago.PagoID,
                        CxCID = fac.CxCID,
                        MontoAplicado = montoAImputar,
                        SaldoAnteriorFactura = saldoActualFac,
                        SaldoPosteriorFactura = fac.SaldoPendiente
                    });

                    remanente -= montoAImputar;
                }

                pago.SaldoFavorGenerado = remanente;

                var cliente = db.Clientes.Find(idCliente);
                if (cliente != null)
                {
                    decimal deudaRestante = db.CuentasPorCobrar
                        .Where(c => c.IdCliente == idCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA"))
                        .Sum(c => c.SaldoPendiente);

                    cliente.SaldoUtilizado = deudaRestante;

                    bool aunTieneMora = db.CuentasPorCobrar
                        .Any(c => c.IdCliente == idCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA") && c.FechaVencimiento < DateTime.Today);

                    if (!aunTieneMora && cliente.EstadoCrediticio == "MOROSO")
                    {
                        cliente.EstadoCrediticio = "ACTIVO";
                    }
                }

                db.SaveChanges();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            finally
            {
                if (manejaContexto) db.Dispose();
            }
        }

        public static void ActualizarEstadosMorosidadDiario()
        {
            try
            {
                using var db = new AppDbContext();
                DateTime hoy = DateTime.Today;

                var facturasPorVencer = db.CuentasPorCobrar
                    .Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento < hoy)
                    .ToList();

                if (facturasPorVencer.Count > 0)
                {
                    foreach (var f in facturasPorVencer)
                    {
                        f.Estado = "VENCIDA";
                    }

                    var idsClientesConMora = facturasPorVencer.Select(f => f.IdCliente).Distinct().ToList();
                    var clientesMora = db.Clientes
                        .Where(c => idsClientesConMora.Contains(c.IdCliente) && c.EstadoCrediticio == "ACTIVO")
                        .ToList();

                    foreach (var c in clientesMora)
                    {
                        c.EstadoCrediticio = "MOROSO";
                    }

                    db.SaveChanges();
                }
            }
            catch { }
        }
    }
}