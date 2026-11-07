using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class NotaCreditoService
    {
        private readonly AppDbContext _db;
        private readonly ConfiguracionDteService _configDteService = new ConfiguracionDteService();

        public NotaCreditoService(AppDbContext db)
        {
            _db = db;
        }

        public NotaCreditoService()
        {
            _db = new AppDbContext();
        }

        // =========================================================================
        // 1. EMISIÓN ESTÁTICA PARA COMPATIBILIDAD DIRECTA
        // =========================================================================
        public static (bool Exito, string Mensaje, int FolioNC) EmitirNotaCredito(
            List<int> idsVentasOriginales,
            string motivo,
            int codigoCausa,
            string usuarioOperador)
        {
            using var db = new AppDbContext();
            var service = new NotaCreditoService(db);
            return service.ProcesarEmisionNotaCredito(idsVentasOriginales, motivo, codigoCausa, usuarioOperador);
        }

        public static (bool Exito, string Mensaje, int FolioND) EmitirNotaDebito(
            int idFacturaOriginal,
            decimal montoRecargo,
            string motivo,
            string usuarioOperador)
        {
            using var db = new AppDbContext();
            var service = new NotaCreditoService(db);
            return service.ProcesarEmisionNotaDebito(idFacturaOriginal, montoRecargo, motivo, usuarioOperador);
        }

        // =========================================================================
        // 2. LÓGICA PRINCIPAL: NOTA DE CRÉDITO ELECTRÓNICA (DTE 61)
        // =========================================================================
        public (bool Exito, string Mensaje, int FolioNC) ProcesarEmisionNotaCredito(
            List<int> idsVentasOriginales,
            string motivo,
            int codigoCausa,
            string usuarioOperador)
        {
            using var tr = _db.Database.BeginTransaction();
            try
            {
                var idsValidos = idsVentasOriginales.Distinct().ToList();
                if (idsValidos.Count == 0)
                {
                    return (false, "No se seleccionaron documentos válidos para anular.", 0);
                }

                var ventas = _db.TVE2607
                    .Include(v => v.Detalles)
                    .Where(v => idsValidos.Contains(v.idTve))
                    .ToList();

                if (ventas.Count == 0)
                {
                    return (false, "No se encontraron los registros de venta en la base de datos.", 0);
                }

                // Regla A: Factura es estrictamente 1 a 1
                bool tieneFactura = ventas.Any(v => v.iddocDTE == 33);
                if (tieneFactura && ventas.Count > 1)
                {
                    return (false, "Las Notas de Crédito sobre Facturas Electrónicas deben emitirse individualmente (1 a 1).", 0);
                }

                // Regla B: Límite máximo de boletas por NC configurado
                int maxBoletas = _configDteService.ObtenerConfiguracion().MaxBoletasPorNC;
                if (!tieneFactura && ventas.Count > maxBoletas)
                {
                    return (false, $"Ha superado el límite de {maxBoletas} boletas configurado por Nota de Crédito.", 0);
                }

                // Obtener rango y folio correlativo activo para Nota de Crédito
                var rangoFolio = _db.Folios.FirstOrDefault(f => f.TipoDocumento.Contains("Nota de Crédito") && f.Activo);
                if (rangoFolio == null)
                {
                    return (false, "No se encontró un rango de folios activo para 'Nota de Crédito Electrónica'.", 0);
                }

                int folioNC = rangoFolio.FolioActual;
                rangoFolio.FolioActual++;

                DateTime ahora = DateTime.Now;
                int ultimoNroInT = _db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;

                decimal sumaTotal = ventas.Sum(v => v.Total);
                decimal sumaNeto = ventas.Sum(v => v.Neto);
                decimal sumaIva = ventas.Sum(v => v.IvA);

                var primerDoc = ventas[0];

                // 1. Insertar encabezado TVE2607 con MONTOS NEGATIVOS para rebajar el LVE
                var tveNC = new TVE2607
                {
                    idLocal = primerDoc.idLocal,
                    nmbLocal = primerDoc.nmbLocal,
                    iddocDTE = 61, // DTE 61: Nota de Crédito Electrónica
                    Documento = "Nota de Crédito Electrónica",
                    FecDoc = ahora,
                    nroDTE = folioNC,
                    nroInT = siguienteNroInT,
                    NroTicket = primerDoc.NroTicket > 0 ? primerDoc.NroTicket : primerDoc.nroDTE,
                    FechaTicket = ahora,
                    SubTotal = -sumaTotal,
                    Descuento = 0,
                    Neto = -sumaNeto,
                    IvA = -sumaIva,
                    Total = -sumaTotal,
                    UserDTE = usuarioOperador,
                    Vendedor = primerDoc.Vendedor,
                    Idcliente = primerDoc.Idcliente,
                    RuT = primerDoc.RuT,
                    RazonSocial = primerDoc.RazonSocial,
                    Giro = primerDoc.Giro,
                    Direccion = primerDoc.Direccion,
                    idcomuna = primerDoc.idcomuna,
                    nComuna = primerDoc.nComuna,
                    idCiudad = primerDoc.idCiudad,
                    nCiudad = primerDoc.nCiudad,
                    Fono1 = primerDoc.Fono1,
                    email = primerDoc.email,
                    status = "Emitido NC",
                    MedioPago = primerDoc.MedioPago,
                    Vuelto = 0,
                    idREF = primerDoc.idREF // Preserva el IdReserva si provenía de un pedido
                };

                _db.TVE2607.Add(tveNC);
                _db.SaveChanges(); // Genera tveNC.idTve

                // 2. Procesar detalles, stock y tabla de auditoría dte_referencias
                foreach (var venta in ventas)
                {
                    // Registrar trazabilidad relacional en dte_referencias
                    var referencia = new DteReferencia
                    {
                        IdTveDTEModificador = tveNC.idTve,
                        TipoDocModificador = 61,
                        NombreDocModificador = "Nota de Crédito Electrónica",
                        FolioModificador = folioNC,

                        IdTveDTEOrigen = venta.idTve,
                        TipoDocOrigen = venta.iddocDTE,
                        NombreDocOrigen = venta.Documento,
                        FolioOrigen = venta.nroDTE,

                        CodigoReferencia = codigoCausa,
                        RazonReferencia = motivo,
                        MontoAfectado = venta.Total,
                        FechaRegistro = ahora,
                        Usuario = usuarioOperador
                    };
                    _db.DteReferencias.Add(referencia);

                    // ========================================================
                    // 🛑 REVERSA DE ABONOS EN PEDIDOS Y RESERVAS (MODO LIBRE)
                    // ========================================================
                    bool debeDevolverStock = true;

                    // 1. Identificar la Reserva: primero por idREF, o como respaldo por el texto del detalle
                    Reserva? reservaAfectada = null;
                    if (venta.idREF.HasValue && venta.idREF.Value > 0)
                    {
                        reservaAfectada = _db.Reservas.FirstOrDefault(r => r.IdReserva == venta.idREF.Value);
                    }

                    // Respaldo inteligente: si idREF no venía en TVE2607, buscar por código de reserva en la glosa
                    if (reservaAfectada == null)
                    {
                        var primerDetalle = _db.TVD2607.FirstOrDefault(d => d.idTve == venta.idTve);
                        string glosaBusqueda = $"{venta.Documento} {primerDetalle?.NmbProducto}";
                        var matchCodigo = System.Text.RegularExpressions.Regex.Match(glosaBusqueda, @"RES-\d{8}-\d{3}");
                        if (matchCodigo.Success)
                        {
                            string codRes = matchCodigo.Value;
                            reservaAfectada = _db.Reservas.FirstOrDefault(r => r.CodigoReserva == codRes);
                        }
                    }

                    // 2. Si es una reserva, aplicar la reversa financiera
                    if (reservaAfectada != null)
                    {
                        // Si la mercadería física no había salido de bodega, no reponer a stock
                        if (!reservaAfectada.StockDescontado)
                        {
                            debeDevolverStock = false;
                        }

                        // Reversar el monto abonado
                        reservaAfectada.TotalAbonado -= venta.Total;
                        if (reservaAfectada.TotalAbonado < 0) reservaAfectada.TotalAbonado = 0;
                        reservaAfectada.SaldoPendiente = Math.Max(0m, reservaAfectada.TotalPedido - reservaAfectada.TotalAbonado);

                        // Registrar la fila negativa en el historial de pagos (reserva_pago)
                        var pagoDevolucion = new ReservaPago
                        {
                            IdReserva = reservaAfectada.IdReserva,
                            Monto = -venta.Total, // Monto negativo
                            MedioPago = $"Devolución NC #{folioNC}",
                            FechaPago = ahora,
                            CajaTurnoID = primerDoc.CajaTurnoID,
                            Usuario = usuarioOperador,
                            EsAbonoInicial = false,
                            EsLiquidacionFinal = false
                        };
                        _db.ReservaPagos.Add(pagoDevolucion);
                    }

                    // Duplicar detalles en TVD2607 en negativo para cuadratura de kardex
                    var detallesOrigen = _db.TVD2607.Where(d => d.idTve == venta.idTve).ToList();
                    foreach (var det in detallesOrigen)
                    {
                        var tvdNC = new TVD2607
                        {
                            idTve = tveNC.idTve,
                            idLocal = det.idLocal,
                            iddocDTE = 61,
                            Documento = "Nota de Crédito Electrónica",
                            NroDTE = folioNC,
                            NroInT = siguienteNroInT,
                            FecMoV = ahora,
                            HoraMoV = ahora.ToString("HH:mm:ss"),
                            IdProducto = det.IdProducto,
                            NmbProducto = det.NmbProducto,
                            nmbCorT = det.nmbCorT,
                            Cantidad = det.Cantidad,
                            Precio = det.Precio,
                            PneTo = det.PneTo,
                            SubTotal = -det.SubTotal,
                            SubNeto = -det.SubNeto,
                            nmbVendedor = det.nmbVendedor,
                            Unidad = det.Unidad
                        };
                        _db.TVD2607.Add(tvdNC);

                        if (debeDevolverStock && det.IdProducto > 0)
                        {
                            var prod = _db.Productos.FirstOrDefault(p => p.ProductoID == det.IdProducto);
                            if (prod != null)
                            {
                                prod.Stock += det.Cantidad;
                                prod.StockDisponible += det.Cantidad;
                            }
                        }
                    }

                    // Marcar documento original como anulado
                    venta.status = "Anulado NC";
                }

                _db.SaveChanges();
                tr.Commit();
                return (true, $"Nota de Crédito N° {folioNC} emitida exitosamente respaldando {ventas.Count} documento(s).", folioNC);
            }
            catch (Exception ex)
            {
                tr.Rollback();
                string detalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return (false, $"Error al emitir Nota de Crédito: {detalle}", 0);
            }
        }

        // =========================================================================
        // 3. LÓGICA PRINCIPAL: NOTA DE DÉBITO ELECTRÓNICA (DTE 56)
        // =========================================================================
        public (bool Exito, string Mensaje, int FolioND) ProcesarEmisionNotaDebito(
            int idFacturaOriginal,
            decimal montoRecargo,
            string motivo,
            string usuarioOperador)
        {
            using var tr = _db.Database.BeginTransaction();
            try
            {
                var factura = _db.TVE2607.FirstOrDefault(v => v.idTve == idFacturaOriginal && v.iddocDTE == 33);
                if (factura == null)
                {
                    return (false, "Solo se puede emitir una Nota de Débito sobre una Factura Electrónica existente (DTE 33).", 0);
                }

                var rangoFolio = _db.Folios.FirstOrDefault(f => f.TipoDocumento.Contains("Nota de Débito") && f.Activo);
                if (rangoFolio == null)
                {
                    return (false, "No se encontró un rango de folios activo para 'Nota de Débito Electrónica'.", 0);
                }

                int folioND = rangoFolio.FolioActual;
                rangoFolio.FolioActual++;

                DateTime ahora = DateTime.Now;
                int ultimoNroInT = _db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;

                decimal neto = Math.Round(montoRecargo / 1.19m, 0);
                decimal iva = montoRecargo - neto;

                // 1. Insertar TVE2607 con MONTOS POSITIVOS (incrementa débito fiscal)
                var tveND = new TVE2607
                {
                    idLocal = factura.idLocal,
                    nmbLocal = factura.nmbLocal,
                    iddocDTE = 56, // DTE 56: Nota de Débito Electrónica
                    Documento = "Nota de Débito Electrónica",
                    FecDoc = ahora,
                    nroDTE = folioND,
                    nroInT = siguienteNroInT,
                    NroTicket = factura.NroTicket > 0 ? factura.NroTicket : factura.nroDTE,
                    FechaTicket = ahora,
                    SubTotal = montoRecargo,
                    Descuento = 0,
                    Neto = neto,
                    IvA = iva,
                    Total = montoRecargo,
                    UserDTE = usuarioOperador,
                    Vendedor = factura.Vendedor,
                    Idcliente = factura.Idcliente,
                    RuT = factura.RuT,
                    RazonSocial = factura.RazonSocial,
                    Giro = factura.Giro,
                    Direccion = factura.Direccion,
                    idcomuna = factura.idcomuna,
                    nComuna = factura.nComuna,
                    idCiudad = factura.idCiudad,
                    nCiudad = factura.nCiudad,
                    Fono1 = factura.Fono1,
                    email = factura.email,
                    status = "Emitido ND",
                    MedioPago = factura.MedioPago,
                    Vuelto = 0
                };

                _db.TVE2607.Add(tveND);
                _db.SaveChanges();

                // 2. Trazabilidad en dte_referencias
                var referencia = new DteReferencia
                {
                    IdTveDTEModificador = tveND.idTve,
                    TipoDocModificador = 56,
                    NombreDocModificador = "Nota de Débito Electrónica",
                    FolioModificador = folioND,

                    IdTveDTEOrigen = factura.idTve,
                    TipoDocOrigen = 33,
                    NombreDocOrigen = "Factura Electrónica",
                    FolioOrigen = factura.nroDTE,

                    CodigoReferencia = 3,
                    RazonReferencia = motivo,
                    MontoAfectado = montoRecargo,
                    FechaRegistro = ahora,
                    Usuario = usuarioOperador
                };
                _db.DteReferencias.Add(referencia);

                // 3. Registrar línea de ajuste en TVD2607
                var tvdND = new TVD2607
                {
                    idTve = tveND.idTve,
                    idLocal = factura.idLocal,
                    iddocDTE = 56,
                    Documento = "Nota de Débito Electrónica",
                    NroDTE = folioND,
                    NroInT = siguienteNroInT,
                    FecMoV = ahora,
                    HoraMoV = ahora.ToString("HH:mm:ss"),
                    IdProducto = 0,
                    NmbProducto = $"Recargo / Ajuste s/ Factura N° {factura.nroDTE}: {motivo}",
                    Cantidad = 1,
                    Precio = montoRecargo,
                    SubTotal = montoRecargo,
                    SubNeto = neto,
                    nmbVendedor = factura.Vendedor,
                    Unidad = "UN"
                };
                _db.TVD2607.Add(tvdND);

                _db.SaveChanges();
                tr.Commit();
                return (true, $"Nota de Débito N° {folioND} emitida exitosamente para la Factura #{factura.nroDTE}.", folioND);
            }
            catch (Exception ex)
            {
                tr.Rollback();
                string detalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return (false, $"Error al emitir Nota de Débito: {detalle}", 0);
            }
        }

        // =========================================================================
        // 4. MÉTODOS HEREDADOS (SOBRECARGAS PARA COMPATIBILIDAD ANTERIOR)
        // =========================================================================
        public static int EmitirNotasCreditoDirecto(IEnumerable<int> idsTveOrigen, string motivo, string codigoREF, bool reponerStock)
        {
            var res = EmitirNotaCredito(idsTveOrigen.ToList(), motivo, int.TryParse(codigoREF, out int c) ? c : 1, "admin");
            return res.Exito ? 1 : 0;
        }

        public bool EmitirNotaCredito(int idTveOrigen, string motivo, string codigoREF, bool reponerStock)
        {
            var res = EmitirNotaCredito(new List<int> { idTveOrigen }, motivo, int.TryParse(codigoREF, out int c) ? c : 1, "admin");
            return res.Exito;
        }
    }
}