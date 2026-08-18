using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ProductoService
    {
        public List<Producto> ObtenerProductosActivos(string filtro = "", string categoria = "Todas", string familia = "Todas")
        {
            using var db = new AppDbContext();
            var query = db.Productos.Where(p => p.Estado).AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                query = query.Where(p => p.Categoria == categoria);
            }

            if (!string.IsNullOrWhiteSpace(familia) && familia != "Todas")
            {
                query = query.Where(p => p.NFamilia == familia);
            }

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string q = filtro.Trim().ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(q) || 
                                         p.CodigoBarra.ToLower().Contains(q) || 
                                         p.ProductoID.ToString().Contains(q));
            }

            return query.OrderBy(p => p.Nombre).ToList();
        }

        public List<string> ObtenerCategoriasRegistradas()
        {
            using var db = new AppDbContext();
            return db.Productos
                .Where(p => p.Estado && !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public List<string> ObtenerFamiliasPorCategoria(string categoria)
        {
            using var db = new AppDbContext();
            var query = db.Productos.Where(p => p.Estado);

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                query = query.Where(p => p.Categoria == categoria);
            }

            return query
                .Where(p => !string.IsNullOrEmpty(p.NFamilia) && p.NFamilia != p.Categoria)
                .Select(p => p.NFamilia!)
                .Distinct()
                .OrderBy(f => f)
                .ToList();
        }

        public decimal ObtenerPrecioSegunCantidad(int productoId, int cantidad, decimal precioBase)
        {
            using var db = new AppDbContext();
            var escala = db.PreciosQ
                .FirstOrDefault(pq => pq.IdProducto == productoId && 
                                      pq.Bloqueo == 0 && 
                                      cantidad >= pq.Qini && 
                                      cantidad <= pq.Qfin);

            return escala != null && escala.NPrecio > 0 ? escala.NPrecio : precioBase;
        }

        public void GuardarProducto(Producto producto, bool esNuevo)
        {
            using var db = new AppDbContext();

            if (string.IsNullOrWhiteSpace(producto.Categoria))
            {
                producto.Categoria = "General";
            }

            if (string.IsNullOrWhiteSpace(producto.NFamilia))
            {
                producto.NFamilia = producto.Categoria;
            }

            producto.FchUpd = DateTime.Now;

            if (esNuevo)
            {
                db.Productos.Add(producto);
            }
            else
            {
                var prodBd = db.Productos.Find(producto.ProductoID);
                if (prodBd != null)
                {
                    prodBd.CodigoBarra = producto.CodigoBarra;
                    prodBd.Nombre = producto.Nombre;
                    prodBd.NmbCorto = producto.NmbCorto;
                    prodBd.Categoria = producto.Categoria;
                    prodBd.NFamilia = producto.NFamilia;
                    prodBd.IdGrupo = producto.IdGrupo;
                    prodBd.IdFamilia = producto.IdFamilia;

                    // Unidades
                    prodBd.UniVenta = producto.UniVenta;
                    prodBd.FactorVenta = producto.FactorVenta;
                    prodBd.UniCosto = producto.UniCosto;
                    prodBd.FactorCompr = producto.FactorCompr;
                    prodBd.Peso = producto.Peso;
                    prodBd.FactorPeso = producto.FactorPeso;

                    // Costos y Precios
                    prodBd.PrecioCosto = producto.PrecioCosto;
                    prodBd.MargenGanancia = producto.MargenGanancia;
                    prodBd.PrecioUnitario = producto.PrecioUnitario;
                    prodBd.Precio2 = producto.Precio2;
                    prodBd.Precio3 = producto.Precio3;
                    prodBd.Precio4 = producto.Precio4;
                    prodBd.Precio5 = producto.Precio5;
                    prodBd.PPP = producto.PPP;
                    prodBd.LstCosto = producto.LstCosto;

                    // Impresión y Ofertas
                    prodBd.IdImpresora = producto.IdImpresora;
                    prodBd.nmbImpreso = producto.nmbImpreso;
                    prodBd.pOferTa = producto.pOferTa;
                    prodBd.FchIni = producto.FchIni;

                    // Stock y Estado
                    prodBd.StockMinimo = producto.StockMinimo;
                    prodBd.ImagenPath = producto.ImagenPath;
                    prodBd.Estado = producto.Estado;
                    prodBd.FchUpd = DateTime.Now;
                    prodBd.Sincro = 0; // Marca para re-sincronizar
                }
            }

            db.SaveChanges();
        }

        public void EliminarProductoLogico(int productoId)
        {
            using var db = new AppDbContext();
            var p = db.Productos.Find(productoId);
            if (p != null)
            {
                p.Estado = false;
                p.FchUpd = DateTime.Now;
                p.Sincro = 0;
                db.SaveChanges();
            }
        }
    }
}