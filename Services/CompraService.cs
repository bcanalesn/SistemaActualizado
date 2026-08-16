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
        public bool RegistrarFacturaCompra(Compra compra, List<DetalleCompra> detalles, bool actualizarPreciosVenta = true)
        {
            if (compra == null) throw new ArgumentNullException(nameof(compra));
            if (detalles == null || detalles.Count == 0) throw new InvalidOperationException("La factura no contiene productos.");

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                // 1. Guardar encabezado
                db.Compras.Add(compra);
                db.SaveChanges(); // Genera el CompraID autoincremental

                // 2. Procesar ítems y actualizar inventario
                foreach (var item in detalles)
                {
                    // Asignar el ID de compra y limpiar navegación para evitar inserciones duplicadas
                    item.CompraID = compra.CompraID;
                    item.Compra = null;
                    item.Producto = null;

                    db.DetalleCompras.Add(item);

                    // Actualizar el producto en BD
                    var producto = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (producto != null)
                    {
                        producto.Stock += item.Cantidad;
                        producto.PrecioCosto = item.PrecioCostoUnitario;

                        if (actualizarPreciosVenta)
                        {
                            decimal margen = producto.MargenGanancia > 0 ? producto.MargenGanancia : 30.00m;
                            decimal netoVenta = producto.PrecioCosto * (1 + (margen / 100m));
                            decimal pvpCalculado = netoVenta * 1.19m;

                            // 🟢 Redondeo a la decena más cercana (Regla de redondeo comercial CLP)
                            producto.PrecioUnitario = Math.Round(pvpCalculado / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
                        }
                    }
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