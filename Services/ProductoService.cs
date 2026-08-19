using System;
using System.Collections.Generic;
using System.Linq;
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

    }
}