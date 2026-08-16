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
                // 1. Guardar encabezado de la compra
                db.Compras.Add(compra);
                db.SaveChanges();

                // 2. Procesar ítems
                foreach (var item in detalles)
                {
                    item.CompraID = compra.CompraID;
                    item.Compra = null;

                    Producto? producto = null;

                    // Si es un producto nuevo creado durante la compra (ID == 0)
                    if (item.ProductoID == 0)
                    {
                        var nuevoProd = new Producto
                        {
                            CodigoBarra = "SKU-" + DateTime.Now.Ticks.ToString().Substring(12),
                            Nombre = item.NombreProducto,
                            Categoria = "General",
                            PrecioCosto = item.PrecioCostoUnitario,
                            MargenGanancia = 30.00m,
                            Stock = item.Cantidad, // Inicia con el stock de esta factura
                            StockMinimo = 5,
                            ImagenPath = "",
                            Estado = true
                        };

                        if (actualizarPreciosVenta)
                        {
                            decimal netoVenta = nuevoProd.PrecioCosto * 1.30m;
                            decimal pvp = netoVenta * 1.19m;
                            nuevoProd.PrecioUnitario = Math.Round(pvp / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
                        }

                        db.Productos.Add(nuevoProd);
                        db.SaveChanges(); // Obtiene su ProductoID real generado por MySQL

                        item.ProductoID = nuevoProd.ProductoID;
                    }
                    else
                    {
                        // Producto existente en catálogo
                        producto = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                        if (producto != null)
                        {
                            producto.Stock += item.Cantidad;
                            producto.PrecioCosto = item.PrecioCostoUnitario;

                            if (actualizarPreciosVenta)
                            {
                                decimal margen = producto.MargenGanancia > 0 ? producto.MargenGanancia : 30.00m;
                                decimal netoVenta = producto.PrecioCosto * (1 + (margen / 100m));
                                decimal pvp = netoVenta * 1.19m;
                                producto.PrecioUnitario = Math.Round(pvp / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
                            }
                        }
                    }

                    item.Producto = null;
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