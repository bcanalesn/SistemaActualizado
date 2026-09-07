using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public decimal VentasCreditoComercial { get; set; }
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

        public int ProcesarCobroTicket(TVE2607 ticket, string tipoDoc, string medioPago, string rutCliente, string razonSocial, string giro, decimal vuelto = 0)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            int iddoc = tipoDoc.Contains("Factura") ? 33 : 39;

            var rangoFolio = _db.Folios.FirstOrDefault(r => r.TipoDocumento == tipoDoc && r.Activo);
            
            int folioOficial;
            if (rangoFolio != null && rangoFolio.FolioActual <= rangoFolio.FolioHasta)
            {
                folioOficial = rangoFolio.FolioActual;
                rangoFolio.FolioActual++;
            }
            else
            {
                folioOficial = (int)(DateTime.Now.Ticks % 100000);
            }

            ticket.iddocDTE = iddoc;
            ticket.Documento = tipoDoc;
            ticket.nroDTE = folioOficial;
            ticket.MedioPago = medioPago;
            ticket.Vuelto = vuelto;
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

            var detalles = _db.TVD2607.Where(d => d.idTve == ticket.idTve).ToList();
            foreach (var item in detalles)
            {
                item.iddocDTE = iddoc;
                item.Documento = tipoDoc;
                item.NroDTE = folioOficial;
                item.NroInT = ticket.nroInT;
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
                var ventasEmitidas = _db.TVE2607
                    .Where(v => v.FecDoc >= fechaApertura && v.status == "Emitido")
                    .ToList();

                decimal totalEfectivo = 0;

                foreach (var v in ventasEmitidas)
                {
                    string medio = v.MedioPago ?? "";

                    if (medio.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        totalEfectivo += (v.iddocDTE == 61 ? -v.Total : v.Total);
                    }
                    else if (medio.StartsWith("Múltiple", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchEfec = Regex.Match(medio, @"Efec:\s*\$?([\d\.,]+)");
                        if (matchEfec.Success && decimal.TryParse(matchEfec.Groups[1].Value.Replace(".", "").Replace(",", ""), out decimal efecMonto))
                        {
                            totalEfectivo += efecMonto - v.Vuelto;
                        }
                    }
                }

                return totalEfectivo;
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
                var ventasEmitidas = _db.TVE2607
                    .Where(v => v.FecDoc >= fechaApertura && v.status == "Emitido")
                    .ToList();

                var anulaciones = _db.TVE2607
                    .Where(v => v.FecDoc >= fechaApertura && v.status == "Anulado")
                    .ToList();

                metricas.VentasTotales = ventasEmitidas.Sum(v => v.Total);
                metricas.CantVentas = ventasEmitidas.Count;

                metricas.MontoAnulaciones = anulaciones.Sum(v => v.Total);
                metricas.CantAnulaciones = anulaciones.Count;

                var idsEmitidos = ventasEmitidas.Select(v => v.idTve).ToList();
                metricas.CantProductos = _db.TVD2607
                    .Where(d => idsEmitidos.Contains(d.idTve))
                    .Sum(d => (int?)d.Cantidad) ?? 0;

                decimal mEfectivo = 0;
                decimal mDebito = 0;
                decimal mTarjetaCredito = 0;
                decimal mCreditoComercial = 0;
                decimal mTransferencia = 0;

                foreach (var v in ventasEmitidas)
                {
                    string medio = (v.MedioPago ?? "").Trim();

                    if (medio.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        mEfectivo += v.Total;
                    }
                    else if (medio.Equals("Crédito Comercial", StringComparison.OrdinalIgnoreCase) || medio.Contains("PLAZO"))
                    {
                        mCreditoComercial += v.Total;
                    }
                    else if (medio.Equals("Débito", StringComparison.OrdinalIgnoreCase))
                    {
                        mDebito += v.Total;
                    }
                    else if (medio.Equals("Tarjeta Crédito", StringComparison.OrdinalIgnoreCase) || medio.Equals("Crédito", StringComparison.OrdinalIgnoreCase))
                    {
                        mTarjetaCredito += v.Total;
                    }
                    else if (medio.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
                    {
                        mTransferencia += v.Total;
                    }
                    else if (medio.StartsWith("Múltiple", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchEfec = Regex.Match(medio, @"Efec:\s*\$?([\d\.,]+)");
                        var matchTarj = Regex.Match(medio, @"Tarj:\s*\$?([\d\.,]+)");
                        var matchTransf = Regex.Match(medio, @"Transf:\s*\$?([\d\.,]+)");

                        decimal ef = 0, tar = 0, tr = 0;
                        if (matchEfec.Success) decimal.TryParse(matchEfec.Groups[1].Value.Replace(".", "").Replace(",", ""), out ef);
                        if (matchTarj.Success) decimal.TryParse(matchTarj.Groups[1].Value.Replace(".", "").Replace(",", ""), out tar);
                        if (matchTransf.Success) decimal.TryParse(matchTransf.Groups[1].Value.Replace(".", "").Replace(",", ""), out tr);

                        mEfectivo += (ef - v.Vuelto);
                        mDebito += tar;
                        mTransferencia += tr;
                    }
                    else
                    {
                        mEfectivo += v.Total;
                    }
                }

                metricas.VentasEfectivo = mEfectivo;
                metricas.VentasTarjetas = mDebito + mTarjetaCredito;
                metricas.VentasTransferencia = mTransferencia;
                metricas.VentasCreditoComercial = mCreditoComercial;

                decimal total = metricas.VentasTotales > 0 ? metricas.VentasTotales : 1;

                if (mEfectivo > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Efectivo", Monto = mEfectivo, Porcentaje = Math.Round((mEfectivo / total) * 100, 1) });
                
                if (mCreditoComercial > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Crédito Comercial", Monto = mCreditoComercial, Porcentaje = Math.Round((mCreditoComercial / total) * 100, 1) });

                if (mDebito > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Débito", Monto = mDebito, Porcentaje = Math.Round((mDebito / total) * 100, 1) });

                if (mTarjetaCredito > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Tarjeta Crédito", Monto = mTarjetaCredito, Porcentaje = Math.Round((mTarjetaCredito / total) * 100, 1) });

                if (mTransferencia > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Transferencia", Monto = mTransferencia, Porcentaje = Math.Round((mTransferencia / total) * 100, 1) });

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