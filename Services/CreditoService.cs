using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ValidacionCreditoResult
    {
        public bool EsValido { get; set; }
        public string MensajeError { get; set; } = string.Empty;
    }

    public static class CreditoService
    {
        /// <summary>
        /// Calcula la fecha de vencimiento sumando únicamente días hábiles (excluye sábados, domingos y feriados).
        /// El cómputo inicia a partir del día siguiente a la emisión (t+1).
        /// </summary>
        public static DateTime CalcularFechaVencimientoHabil(DateTime fechaEmision, int diasHabiles, HashSet<DateTime> feriados)
        {
            if (diasHabiles <= 0) return fechaEmision.Date;

            DateTime fechaActual = fechaEmision.Date;
            int diasContados = 0;

            while (diasContados < diasHabiles)
            {
                fechaActual = fechaActual.AddDays(1);

                // Excluir sábados y domingos
                if (fechaActual.DayOfWeek == DayOfWeek.Saturday || fechaActual.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Excluir feriados registrados en la BD
                if (feriados.Contains(fechaActual.Date))
                    continue;

                diasContados++;
            }

            return fechaActual;
        }

        /// <summary>
        /// Valida si un cliente cumple todas las condiciones crediticias para emitirle una factura a crédito.
        /// </summary>
        public static ValidacionCreditoResult ValidarVentaCredito(Cliente cliente, decimal totalVenta, AppDbContext db)
        {
            if (cliente == null || !cliente.Estado)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente se encuentra inactivo o no está registrado." };

            int diasEfectivos = cliente.DiasCreditoHabiles > 0 ? cliente.DiasCreditoHabiles : cliente.DiasCredito;

            // Si tiene días asignados (> 0), se considera habilitado para crédito
            if (diasEfectivos <= 0 && !cliente.PermiteCredito)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente no tiene habilitada la línea de crédito comercial ni días asignados." };

            if (cliente.ModalidadPago == "SOLO_CONTADO" && diasEfectivos <= 0)
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente está configurado exclusivamente para compras al contado." };

            if (cliente.EstadoCrediticio == "BLOQUEADO" || cliente.EstadoCrediticio == "SUSPENDIDO")
                return new ValidacionCreditoResult { EsValido = false, MensajeError = $"Crédito denegado. El cliente presenta estado crediticio: '{cliente.EstadoCrediticio}'." };

            // 1. Validar facturas vencidas impagas
            DateTime hoy = DateTime.Today;
            bool tieneMora = db.CuentasPorCobrar
                .Any(c => c.IdCliente == cliente.IdCliente &&
                        (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA") &&
                        c.FechaVencimiento < hoy);

            if (tieneMora)
            {
                return new ValidacionCreditoResult { EsValido = false, MensajeError = "El cliente mantiene documentos vencidos pendientes de pago. Debe regularizar su deuda para continuar facturando a crédito." };
            }

            // 2. Validar disponibilidad de cupo de crédito
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

        /// <summary>
        /// Inserta el documento por cobrar en cuentas_por_cobrar al confirmar una venta/factura a crédito.
        /// </summary>
        public static void RegistrarFacturaCredito(int ventaId, int folioFactura, DateTime fechaEmision, Cliente cliente, decimal totalFactura, string usuarioEmisor, AppDbContext db)
        {
            var feriados = db.Feriados.Select(f => f.Fecha.Date).ToHashSet();
            int diasHabiles = cliente.DiasCreditoHabiles > 0 ? cliente.DiasCreditoHabiles : cliente.DiasCredito;

            DateTime fechaVenc = CalcularFechaVencimientoHabil(fechaEmision, diasHabiles, feriados);

            var cxc = new CuentaPorCobrar
            {
                VentaID = ventaId,
                IdCliente = cliente.IdCliente,
                TipoDTE = 33, // Factura Electrónica
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

            // Actualizar saldo utilizado en ficha del cliente
            cliente.SaldoUtilizado += totalFactura;

            db.SaveChanges();
        }

        /// <summary>
        /// Procesa un pago o abono aplicando método FIFO (factura más antigua primero) o imputación manual.
        /// </summary>
        public static void ProcesarPagoCliente(int idCliente, decimal montoPago, string medioPago, string nroComprobante, string observaciones, string usuarioCobrador, List<int>? cxcIdsSeleccionadas, AppDbContext db)
        {
            if (montoPago <= 0) throw new ArgumentException("El monto a abonar debe ser superior a cero.");

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

                pago.SaldoFavorGenerado = remanente; // Excedente a favor del cliente si sobró dinero

                // Recalcular saldo utilizado total del cliente
                var cliente = db.Clientes.Find(idCliente);
                if (cliente != null)
                {
                    decimal deudaRestante = db.CuentasPorCobrar
                        .Where(c => c.IdCliente == idCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA"))
                        .Sum(c => c.SaldoPendiente);

                    cliente.SaldoUtilizado = deudaRestante;

                    // Si ya no tiene facturas vencidas y estaba en mora, restablecer a ACTIVO
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
        }

        /// <summary>
        /// Proceso diario: evalúa facturas vencidas y actualiza el estado de los clientes a 'MOROSO'.
        /// </summary>
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