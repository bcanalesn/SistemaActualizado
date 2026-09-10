using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ReporteService
    {
        public ResumenReporteVentasDTO ObtenerResumenVentas(DateTime desde, DateTime hasta)
        {
            using var db = new AppDbContext();

            var ventas = db.TVE2607
                .AsNoTracking()
                .Where(v => v.FecDoc >= desde && v.FecDoc <= hasta && v.status != "Anulado")
                .OrderByDescending(v => v.FecDoc)
                .Select(v => new ItemReporteVentaDTO
                {
                    IdTve = v.idTve,
                    NroDTE = v.nroDTE,
                    FecDoc = v.FecDoc,
                    Documento = v.Documento,
                    Total = v.Total,
                    RazonSocial = v.RazonSocial,
                    RuT = v.RuT,
                    MedioPago = v.MedioPago,
                    Vendedor = v.Vendedor ?? v.UserDTE
                })
                .ToList();

            decimal total = ventas.Sum(v => v.Total);
            int cantidad = ventas.Count;
            decimal ticketPromedio = cantidad > 0 ? total / cantidad : 0m;

            return new ResumenReporteVentasDTO
            {
                TotalRecaudado = total,
                CantidadVentas = cantidad,
                TicketPromedio = ticketPromedio,
                Ventas = ventas
            };
        }
    }
}