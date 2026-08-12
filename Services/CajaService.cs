using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class DesgloseMedioPago
    {
        public string Medio { get; set; } = "Efectivo";
        public decimal Monto { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class MetricasTurno
    {
        public decimal VentasTotales { get; set; }
        public decimal VentasEfectivo { get; set; }
        public decimal VentasTarjetas { get; set; }
        public decimal VentasTransferencia { get; set; }
        public decimal MontoAnulaciones { get; set; }
        public int CantVentas { get; set; }
        public int CantAnulaciones { get; set; }
        public int CantProductos { get; set; }
        public List<DesgloseMedioPago> ListaDesglose { get; set; } = new List<DesgloseMedioPago>();
    }

    public class CajaService
    {
        private readonly AppDbContext _db = new AppDbContext();

        public List<TVE2607> ObtenerTicketsPendientes(string filtro = "")
        {
            var query = _db.TVE2607.Where(v => v.status == "Pendiente");

            if (!string.IsNullOrEmpty(filtro))
            {
                query = query.Where(v => v.nroDTE.ToString().Contains(filtro));
            }

            return query.OrderByDescending(v => v.FecDoc).ToList();
        }

        public List<TVD2607> ObtenerDetallesTicket(int idTve)
        {
            return _db.TVD2607.Where(d => d.idTve == idTve).ToList();
        }

        public int ProcesarCobroTicket(TVE2607 ticket, string tipoDoc, string medioPago, string rutCliente, string razonSocial, string giro)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            int iddoc = tipoDoc.Contains("Factura") ? 33 : 39;

            // 1. OBTENER EL RANGO ACTIVO DE LA TABLA FOLIOS
            var rangoFolio = _db.Folios.FirstOrDefault(r => r.TipoDocumento == tipoDoc && r.Activo);
            
            int folioOficial;
            if (rangoFolio != null && rangoFolio.FolioActual <= rangoFolio.FolioHasta)
            {
                folioOficial = rangoFolio.FolioActual;
                rangoFolio.FolioActual++; // AVANZA EL FOLIO ACTUAL PARA LA SIGUIENTE VENTA
            }
            else
            {
                // Respaldo por si no hay folios configurados
                folioOficial = (int)(DateTime.Now.Ticks % 100000);
            }

            // 2. ACTUALIZAR ENCABEZADO DE VENTA (TVE2607)
            ticket.iddocDTE = iddoc;
            ticket.Documento = tipoDoc;
            ticket.nroDTE = folioOficial; // Folio fiscal consumido
            ticket.UserDTE = medioPago;
            ticket.status = "Emitido";

            if (tipoDoc.Contains("Factura"))
            {
                ticket.RuT = string.IsNullOrWhiteSpace(rutCliente) ? "76.543.210-K" : rutCliente;
                ticket.RazonSocial = string.IsNullOrWhiteSpace(razonSocial) ? "SIN RAZON SOCIAL" : razonSocial;
                ticket.Giro = string.IsNullOrWhiteSpace(giro) ? "GENERAL" : giro;
            }
            else
            {
                ticket.RuT = string.IsNullOrWhiteSpace(rutCliente) ? "66.666.666-6" : rutCliente;
                ticket.RazonSocial = string.IsNullOrWhiteSpace(razonSocial) ? "Consumidor Final" : razonSocial;
            }

            // 3. ACTUALIZAR DETALLE DE VENTA (TVD2607)
            var detalles = _db.TVD2607.Where(d => d.idTve == ticket.idTve).ToList();
            foreach (var item in detalles)
            {
                item.iddocDTE = iddoc;
                item.Documento = tipoDoc;
                item.NroDTE = folioOficial;
                item.NroInT = ticket.nroInT; // Preserva el nroInT correlativo interno
            }

            _db.SaveChanges();
            return folioOficial;
    }

        public void AnularTicket(int idTve)
        {
            var ticket = _db.TVE2607.FirstOrDefault(t => t.idTve == idTve);
            if (ticket == null) return;

            ticket.status = "Anulado";

            var detalles = _db.TVD2607.Where(d => d.idTve == idTve).ToList();
            foreach (var item in detalles)
            {
                var prodBD = _db.Productos.FirstOrDefault(p => p.ProductoID == item.IdProducto);
                if (prodBD != null)
                {
                    prodBD.Stock += item.Cantidad;
                }
            }

            _db.SaveChanges();
        }

        public decimal CalcularVentasEfectivo(DateTime fechaApertura)
        {
            try
            {
                return _db.TVE2607
                    .Where(v => v.FecDoc >= fechaApertura && v.status == "Emitido" && (string.IsNullOrEmpty(v.UserDTE) || v.UserDTE.Contains("Efectivo") || v.UserDTE.Contains("Múltiple") || v.UserDTE.Contains("Cajero")))
                    .Sum(v => (decimal?)(v.iddocDTE == 61 ? -v.Total : v.Total)) ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public MetricasTurno ObtenerMetricasResumenTurno(DateTime fechaApertura)
        {
            var metricas = new MetricasTurno();
            try
            {
                var ventasEmitidas = _db.TVE2607.Where(v => v.FecDoc >= fechaApertura && v.status == "Emitido").ToList();
                var anulaciones = _db.TVE2607.Where(v => v.FecDoc >= fechaApertura && v.status == "Anulado").ToList();

                metricas.VentasTotales = ventasEmitidas.Sum(v => v.Total);
                metricas.CantVentas = ventasEmitidas.Count;

                metricas.MontoAnulaciones = anulaciones.Sum(v => v.Total);
                metricas.CantAnulaciones = anulaciones.Count;

                var idsEmitidos = ventasEmitidas.Select(v => v.idTve).ToList();
                metricas.CantProductos = _db.TVD2607.Where(d => idsEmitidos.Contains(d.idTve)).Sum(d => (int?)d.Cantidad) ?? 0;

                // Desglose por Medio de Pago en Base al UserDTE / Documento
                decimal mEfectivo = 0;
                decimal mDebito = 0;
                decimal mCredito = 0;
                decimal mTransferencia = 0;
                decimal mMultiple = 0;

                foreach (var v in ventasEmitidas)
            {
                string infoPago = v.UserDTE ?? "";

                if (infoPago.Contains("Débito") || infoPago.Contains("Debito"))
                {
                    mDebito += v.Total;
                }
                else if (infoPago.Contains("Crédito") || infoPago.Contains("Credito"))
                {
                    mCredito += v.Total;
                }
                else if (infoPago.Contains("Transferencia"))
                {
                    mTransferencia += v.Total;
                }
                else
                {
                    // Todo lo demás (Efectivo y la porción cobrada directamente) computa a Efectivo
                    mEfectivo += v.Total;
                }
            }

            metricas.VentasEfectivo = mEfectivo;
            metricas.VentasTarjetas = mDebito + mCredito;
            metricas.VentasTransferencia = mTransferencia;

            decimal total = metricas.VentasTotales > 0 ? metricas.VentasTotales : 1;

            // Solo mantenemos los 4 medios principales en el desglose
            if (mEfectivo > 0) metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Efectivo", Monto = mEfectivo, Porcentaje = Math.Round((mEfectivo / total) * 100, 0) });
            if (mDebito > 0) metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Débito", Monto = mDebito, Porcentaje = Math.Round((mDebito / total) * 100, 0) });
            if (mCredito > 0) metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Crédito", Monto = mCredito, Porcentaje = Math.Round((mCredito / total) * 100, 0) });
            if (mTransferencia > 0) metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Transferencia", Monto = mTransferencia, Porcentaje = Math.Round((mTransferencia / total) * 100, 0) });

            if (metricas.ListaDesglose.Count == 0)
            {
                metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Efectivo", Monto = 0, Porcentaje = 100 });
            }
            }
            catch { }

            return metricas;
        }
    }
}