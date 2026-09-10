using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class LibroContableService
    {
        public ResumenLibroVentasDTO ObtenerLibroVentas(DateTime desde, DateTime hasta)
        {
            using var db = new AppDbContext();

            // Solo documentos tributarios oficiales (se excluyen tickets preliminares)
            var ventas = db.TVE2607
                .AsNoTracking()
                .Where(v => v.FecDoc >= desde && v.FecDoc <= hasta && 
                            v.Documento != "Ticket de Atención" && 
                            v.iddocDTE != 0)
                .OrderByDescending(v => v.FecDoc)
                .ToList();

            var validas = ventas.Where(v => !v.status.Contains("Anulado")).ToList();
            var boletas = validas.Where(v => v.Documento.Contains("Boleta")).ToList();
            var facturas = validas.Where(v => v.Documento.Contains("Factura")).ToList();
            var notasCredito = ventas.Where(v => v.iddocDTE == 61 || v.Documento.Contains("Crédito")).ToList();

            decimal totalVentas = validas.Where(v => v.iddocDTE != 61).Sum(v => v.Total);
            int cantDocs = validas.Count;

            decimal netoTotal = validas.Where(v => v.iddocDTE != 61).Sum(v => v.Neto);
            decimal ivaBoletas = boletas.Sum(v => v.IvA);
            decimal ivaFacturas = facturas.Sum(v => v.IvA);
            decimal ivaNotasCredito = notasCredito.Sum(v => v.IvA);
            decimal debitoFiscalTotal = (ivaFacturas + ivaBoletas) - ivaNotasCredito;

            return new ResumenLibroVentasDTO
            {
                TotalVentas = totalVentas,
                CantidadDocumentos = cantDocs,
                CantidadBoletas = boletas.Count,
                PorcentajeBoletas = cantDocs > 0 ? Math.Round((decimal)boletas.Count / cantDocs * 100, 0) : 0,
                CantidadFacturas = facturas.Count,
                PorcentajeFacturas = cantDocs > 0 ? Math.Round((decimal)facturas.Count / cantDocs * 100, 0) : 0,
                CantidadNotasCredito = notasCredito.Count,
                MontoNotasCredito = notasCredito.Sum(d => d.Total),
                TotalNeto = netoTotal,
                DebitoFiscalTotal = debitoFiscalTotal,
                Ventas = ventas
            };
        }

        public ResumenLibroComprasDTO ObtenerLibroCompras(DateTime fDesde, DateTime fHasta, string filtroTexto = "", string tipoDoc = "Todos", string estado = "Todos")
        {
            using var db = new AppDbContext();

            var query = db.Compras
                .AsNoTracking()
                .Where(c => c.FechaEmision >= fDesde && c.FechaEmision <= fHasta)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtroTexto))
            {
                string filtro = filtroTexto.Trim().ToLower();
                query = query.Where(c => c.RazonSocialProveedor.ToLower().Contains(filtro) ||
                                         c.RutProveedor.ToLower().Contains(filtro) ||
                                         c.NroFacturaProveedor.ToString().Contains(filtro));
            }

            if (tipoDoc != "Todos")
            {
                query = query.Where(c => c.TipoDocumento == tipoDoc);
            }

            if (estado != "Todos")
            {
                query = query.Where(c => c.Estado == estado);
            }

            var compras = query.OrderByDescending(c => c.FechaEmision).ToList();

            return new ResumenLibroComprasDTO
            {
                TotalBruto = compras.Sum(c => c.MontoTotal),
                TotalNeto = compras.Sum(c => c.MontoNeto),
                TotalIva = compras.Sum(c => c.MontoIva),
                TotalFacturas = compras.Count,
                TotalProveedores = compras.Select(c => c.RutProveedor).Distinct().Count(),
                Compras = compras
            };
        }
    }
}