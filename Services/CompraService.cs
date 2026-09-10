using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class CompraService
    {
        public Compra? ObtenerCompraPorId(int compraId)
        {
            using var db = new AppDbContext();
            return db.Compras.AsNoTracking().FirstOrDefault(c => c.CompraID == compraId);
        }

        public List<DetalleCompra> ObtenerDetallesCompra(int compraId)
        {
            using var db = new AppDbContext();
            return db.DetalleCompras.AsNoTracking().Where(d => d.CompraID == compraId).ToList();
        }

        public bool RegistrarFacturaCompra(Compra compra, List<DetalleCompra> detalles, bool actualizarPreciosVenta = true)
        {
            if (compra == null) throw new ArgumentNullException(nameof(compra));
            if (detalles == null || detalles.Count == 0) throw new InvalidOperationException("El documento no contiene líneas de detalle.");

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                db.Compras.Add(compra);
                db.SaveChanges();

                foreach (var item in detalles)
                {
                    item.CompraID = compra.CompraID;
                    item.Compra = null;
                    item.Producto = null;

                    if (item.AfectaStock && item.ProductoID.HasValue && item.ProductoID.Value > 0)
                    {
                        var producto = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID.Value);
                        if (producto != null)
                        {
                            producto.Stock += item.Cantidad;

                            if (actualizarPreciosVenta && item.PrecioCostoUnitario > 0)
                            {
                                ProductoService.AplicarNuevoCostoYRecalcularListas(producto, item.PrecioCostoUnitario);
                            }
                            else
                            {
                                producto.PrecioCosto = item.PrecioCostoUnitario;
                            }
                        }
                    }

                    db.DetalleCompras.Add(item);
                }

                db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                string inner = ex.InnerException != null ? $"\nDetalle: {ex.InnerException.Message}" : "";
                throw new Exception($"{ex.Message}{inner}");
            }
        }
    }
}