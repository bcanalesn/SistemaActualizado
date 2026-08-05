using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class NotaCreditoService
    {
        private readonly AppDbContext _db;

        public NotaCreditoService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Emite una Nota de Crédito Electrónica (DTE 61) devolviendo el stock exacto según las cantidades vendidas.
        /// </summary>
        public bool EmitirNotaCredito(int idTveOrigen, string motivo, string codigoREF, bool reponerStock)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                // 1. Obtener la venta de origen en TVE2607
                var ventaOrigen = _db.TVE2607.FirstOrDefault(v => v.idTve == idTveOrigen);
                if (ventaOrigen == null || ventaOrigen.status == "Anulado") return false;

                // 2. Obtener los detalles asociados desde TVD2607
                var detallesOrigen = _db.TVD2607.Where(d => d.idTve == idTveOrigen).ToList();

                // 3. Crear el nuevo encabezado DTE para la Nota de Crédito (DTE 61)
                var ncHeader = new TVE2607
                {
                    idLocal = ventaOrigen.idLocal,
                    nmbLocal = ventaOrigen.nmbLocal,
                    iddocDTE = 61, // 61 = Nota de Crédito Electrónica (SII)
                    Documento = "Nota de Crédito Electrónica",
                    nroDTE = (int)(DateTime.Now.Ticks % 1000000),
                    FecDoc = DateTime.Now,
                    SubTotal = ventaOrigen.SubTotal,
                    Descuento = ventaOrigen.Descuento,
                    Neto = ventaOrigen.Neto,
                    IvA = ventaOrigen.IvA,
                    Total = ventaOrigen.Total,
                    UserDTE = ventaOrigen.UserDTE,
                    Vendedor = ventaOrigen.Vendedor,
                    RuT = ventaOrigen.RuT,
                    RazonSocial = ventaOrigen.RazonSocial,
                    Giro = ventaOrigen.Giro,
                    status = "Emitido",
                    idREF = ventaOrigen.idTve,
                    nroREF = ventaOrigen.nroDTE,
                    codigoREF = codigoREF
                };

                // Marcar la venta original con referencia a la anulacion/correccion
                ventaOrigen.status = "Nota de Crédito Emitida";

                _db.TVE2607.Add(ncHeader);
                _db.SaveChanges(); // Persistir para obtener idTve

                // 4. Copiar cada ítem conservando la CANTIDAD EXACTA vendida
                foreach (var det in detallesOrigen)
                {
                    var ncDetalle = new TVD2607
                    {
                        idTve = ncHeader.idTve,
                        idLocal = det.idLocal,
                        iddocDTE = 61,
                        Documento = "Nota de Crédito Electrónica",
                        IdProducto = det.IdProducto,
                        NmbProducto = det.NmbProducto,
                        Cantidad = det.Cantidad, // Se respeta la cantidad original (ej: 3 unidades)
                        Precio = det.Precio,
                        SubTotal = det.SubTotal
                    };

                    _db.TVD2607.Add(ncDetalle);

                    // 5. Reintegrar la CANTIDAD COMPLETA al stock en la tabla productos
                    if (reponerStock)
                    {
                        var producto = _db.Productos.FirstOrDefault(p => p.ProductoID == det.IdProducto);
                        if (producto != null)
                        {
                            producto.Stock += det.Cantidad; // Devuelve las 3 unidades completas al inventario
                        }
                    }
                }

                _db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}