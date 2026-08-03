using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public static class NotaCreditoService
    {
        public const string TipoDocumentoNotaCredito = "Nota de Crédito Electrónica";

        public static Venta ProcesarNotaCredito(
            AppDbContext db,
            int ventaOrigenId,
            string codigoCausa,
            string glosa,
            bool reintegrarInventario,
            string usuarioActual)
        {
            var ventaOrigen = db.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefault(v => v.VentaID == ventaOrigenId)
                ?? throw new InvalidOperationException($"No se encontró la venta origen #{ventaOrigenId}.");

            if (string.Equals(ventaOrigen.TipoDocumento, TipoDocumentoNotaCredito, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La venta seleccionada ya corresponde a una Nota de Crédito.");
            }

            bool yaPoseeNotaCredito = db.Ventas.Any(v =>
                v.idREF == ventaOrigen.VentaID &&
                string.Equals(v.TipoDocumento, TipoDocumentoNotaCredito, StringComparison.OrdinalIgnoreCase));

            if (yaPoseeNotaCredito)
            {
                throw new InvalidOperationException("La venta seleccionada ya posee una Nota de Crédito asociada.");
            }

            using var transaccion = db.Database.BeginTransaction();

            int folioNotaCredito = ObtenerSiguienteFolio(db, TipoDocumentoNotaCredito);

            var notaCredito = new Venta
            {
                Fecha = DateTime.Now,
                Total = ventaOrigen.Total,
                Neto = ventaOrigen.Neto,
                IVA = ventaOrigen.IVA,
                MedioPago = "Nota de Crédito",
                Usuario = usuarioActual,
                TipoDocumento = TipoDocumentoNotaCredito,
                FolioDTE = folioNotaCredito,
                RutCliente = ventaOrigen.RutCliente,
                RazonSocial = ventaOrigen.RazonSocial,
                Giro = ventaOrigen.Giro,
                Direccion = ventaOrigen.Direccion,
                Comuna = ventaOrigen.Comuna,
                Ciudad = ventaOrigen.Ciudad,
                EstadoDTE = "Aceptado_SII",
                idREF = ventaOrigen.VentaID,
                nroREF = ventaOrigen.FolioDTE,
                codigoREF = codigoCausa,
                GlosaREF = glosa
            };

            db.Ventas.Add(notaCredito);
            db.SaveChanges();

            var detallesNC = new List<VentaDetalle>();
            foreach (var detalleOrigen in ventaOrigen.Detalles)
            {
                detallesNC.Add(new VentaDetalle
                {
                    VentaID = notaCredito.VentaID,
                    ProductoID = detalleOrigen.ProductoID,
                    CodigoBarra = detalleOrigen.CodigoBarra,
                    NombreProducto = detalleOrigen.NombreProducto,
                    PrecioUnitario = detalleOrigen.PrecioUnitario,
                    Cantidad = detalleOrigen.Cantidad,
                    Subtotal = detalleOrigen.Subtotal
                });
            }

            if (detallesNC.Count > 0)
            {
                db.VentaDetalles.AddRange(detallesNC);
            }

            ventaOrigen.EstadoDTE = "Anulado_NC";
            ventaOrigen.idREF = notaCredito.VentaID;
            ventaOrigen.nroREF = notaCredito.FolioDTE;
            ventaOrigen.codigoREF = codigoCausa;
            ventaOrigen.GlosaREF = glosa;

            if (reintegrarInventario && string.Equals(codigoCausa, "1", StringComparison.OrdinalIgnoreCase))
            {
                ReintegrarInventario(db, ventaOrigen.Detalles);
            }

            db.SaveChanges();
            transaccion.Commit();

            return notaCredito;
        }

        private static int ObtenerSiguienteFolio(AppDbContext db, string tipoDocumento)
        {
            var rangoFolio = db.Folios.FirstOrDefault(f => f.TipoDocumento == tipoDocumento && f.Activo);

            if (rangoFolio == null)
            {
                throw new InvalidOperationException($"No existe un rango activo de folios para {tipoDocumento}.");
            }

            int folioAsignado = rangoFolio.FolioActual;
            if (folioAsignado < rangoFolio.FolioDesde || folioAsignado > rangoFolio.FolioHasta)
            {
                throw new InvalidOperationException($"El rango de folios para {tipoDocumento} está fuera de límites.");
            }

            rangoFolio.FolioActual += 1;
            if (rangoFolio.FolioActual > rangoFolio.FolioHasta)
            {
                rangoFolio.Activo = false;
            }

            return folioAsignado;
        }

        private static void ReintegrarInventario(AppDbContext db, IEnumerable<VentaDetalle> detalles)
        {
            foreach (var detalle in detalles)
            {
                var producto = db.Productos.FirstOrDefault(p => p.ProductoID == detalle.ProductoID);
                if (producto != null)
                {
                    producto.Stock += detalle.Cantidad;
                }
            }
        }
    }
}