using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
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

        public int ProcesarCobroTicket(TVE2607 ticket, string tipoDoc, string usuarioNombre, string rutCliente, string razonSocial, string giro)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            int folioOficial = (int)(DateTime.Now.Ticks % 1000000);
            int iddoc = tipoDoc.Contains("Factura") ? 33 : 39;

            ticket.iddocDTE = iddoc;
            ticket.Documento = tipoDoc;
            ticket.nroDTE = folioOficial;
            ticket.UserDTE = string.IsNullOrWhiteSpace(usuarioNombre) ? "Cajero" : usuarioNombre;
            ticket.status = "Emitido";

            if (tipoDoc.Contains("Factura"))
            {
                ticket.RuT = string.IsNullOrWhiteSpace(rutCliente) ? "76.543.210-K" : rutCliente;
                ticket.RazonSocial = string.IsNullOrWhiteSpace(razonSocial) ? "SIN RAZON SOCIAL" : razonSocial;
                ticket.Giro = string.IsNullOrWhiteSpace(giro) ? "GENERAL" : giro;
            }
            else
            {
                if (string.IsNullOrEmpty(ticket.RuT)) ticket.RuT = "66.666.666-6";
                if (string.IsNullOrEmpty(ticket.RazonSocial)) ticket.RazonSocial = "Consumidor Final";
            }

            var detalles = _db.TVD2607.Where(d => d.idTve == ticket.idTve).ToList();
            foreach (var item in detalles)
            {
                item.iddocDTE = iddoc;
                item.Documento = tipoDoc;
                item.NroDTE = folioOficial;
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
                    .Where(v => v.FecDoc >= fechaApertura && v.status == "Emitido")
                    .Sum(v => (decimal?)(v.iddocDTE == 61 ? -v.Total : v.Total)) ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}