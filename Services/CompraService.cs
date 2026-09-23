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

        public bool ActualizarFacturaCompra(Compra compraModificada, List<DetalleCompra> nuevosDetalles)
        {
            if (compraModificada == null) throw new ArgumentNullException(nameof(compraModificada));
            if (nuevosDetalles == null || nuevosDetalles.Count == 0) 
                throw new InvalidOperationException("El documento debe contener al menos un ítem.");

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var compraBd = db.Compras.FirstOrDefault(c => c.CompraID == compraModificada.CompraID);
                if (compraBd == null) throw new InvalidOperationException("La compra a editar no existe en la base de datos.");

                // 1. OBTENER DETALLES ANTIGUOS Y REVERTIR EL STOCK PREVIO
                var detallesAntiguos = db.DetalleCompras.Where(d => d.CompraID == compraModificada.CompraID).ToList();
                if (compraBd.TipoCompra == "MERCADERIA")
                {
                    foreach (var itemViejo in detallesAntiguos)
                    {
                        if (itemViejo.AfectaStock && itemViejo.ProductoID.HasValue && itemViejo.ProductoID.Value > 0)
                        {
                            var prodViejo = db.Productos.FirstOrDefault(p => p.ProductoID == itemViejo.ProductoID.Value);
                            if (prodViejo != null)
                            {
                                prodViejo.Stock -= itemViejo.Cantidad;
                                if (prodViejo.Stock < 0) prodViejo.Stock = 0;
                            }
                        }
                    }
                }

                // 2. ELIMINAR LOS REGISTROS ANTIGUOS DE DETALLE
                db.DetalleCompras.RemoveRange(detallesAntiguos);
                db.SaveChanges();

                // 3. RECALCULAR TOTALES DEL DOCUMENTO
                decimal neto = nuevosDetalles.Sum(d => d.Subtotal);
                decimal iva = Math.Round(neto * 0.19m);
                decimal total = neto + iva;

                // 4. ACTUALIZAR CABECERA DE COMPRA
                compraBd.TipoDocumento = compraModificada.TipoDocumento;
                compraBd.NroFacturaProveedor = compraModificada.NroFacturaProveedor;
                compraBd.RutProveedor = compraModificada.RutProveedor;
                compraBd.RazonSocialProveedor = compraModificada.RazonSocialProveedor;
                compraBd.FechaEmision = compraModificada.FechaEmision;
                compraBd.Estado = compraModificada.Estado;
                compraBd.MontoNeto = neto;
                compraBd.MontoIva = iva;
                compraBd.MontoTotal = total;

                // 5. INSERTAR NUEVOS DETALLES Y APLICAR NUEVO STOCK/PRECIOS
                foreach (var itemNuevo in nuevosDetalles)
                {
                    
                    itemNuevo.CompraID = compraBd.CompraID;
                    itemNuevo.Compra = null;
                    itemNuevo.Producto = null;

                    if (compraBd.TipoCompra == "MERCADERIA" && itemNuevo.AfectaStock && itemNuevo.ProductoID.HasValue && itemNuevo.ProductoID.Value > 0)
                    {
                        var prodNuevo = db.Productos.FirstOrDefault(p => p.ProductoID == itemNuevo.ProductoID.Value);
                        if (prodNuevo != null)
                        {
                            prodNuevo.Stock += itemNuevo.Cantidad;

                            if (itemNuevo.PrecioCostoUnitario > 0)
                            {
                                ProductoService.AplicarNuevoCostoYRecalcularListas(prodNuevo, itemNuevo.PrecioCostoUnitario);
                            }
                        }
                    }

                    db.DetalleCompras.Add(itemNuevo);
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