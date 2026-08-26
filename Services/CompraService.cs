using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class CompraService
    {
        public bool RegistrarFacturaCompra(Compra compra, List<DetalleCompra> detalles, bool actualizarPreciosVenta = true)
        {
            if (compra == null) throw new ArgumentNullException(nameof(compra));
            if (detalles == null || detalles.Count == 0) throw new InvalidOperationException("El documento no contiene líneas de detalle.");

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                // 1. Guardar cabecera
                db.Compras.Add(compra);
                db.SaveChanges();

                // 2. Procesar líneas
                foreach (var item in detalles)
                {
                    item.CompraID = compra.CompraID;
                    item.Compra = null;
                    item.Producto = null;

                    // SOLO SI ES MERCADERÍA AFECTA STOCK Y PRECIOS
                    if (item.AfectaStock && item.ProductoID.HasValue && item.ProductoID.Value > 0)
                    {
                        var producto = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID.Value);
                        if (producto != null)
                        {
                            producto.Stock += item.Cantidad;
                            producto.PrecioCosto = item.PrecioCostoUnitario;

                            if (actualizarPreciosVenta && item.PvpSugerido > 0)
                            {
                                producto.PrecioUnitario = item.PvpSugerido;
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