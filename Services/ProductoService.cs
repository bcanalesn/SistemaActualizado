using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ProductoService
    {
        private readonly AppDbContext _db = new AppDbContext();

        public List<Producto> ObtenerProductos(string filtro = "")
        {
            var query = _db.Productos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(p => (p.Nombre != null && p.Nombre.Contains(filtro)) || 
                                         (p.CodigoBarra != null && p.CodigoBarra.Contains(filtro)));
            }

            return query.OrderBy(p => p.Nombre).ToList();
        }

        public void GuardarProducto(Producto producto, bool esNuevo)
        {
            if (esNuevo)
            {
                _db.Productos.Add(producto);
            }
            _db.SaveChanges();
        }

        public void CambiarEstado(int productoId)
        {
            var p = _db.Productos.FirstOrDefault(x => x.ProductoID == productoId);
            if (p != null)
            {
                p.Estado = !p.Estado;
                _db.SaveChanges();
            }
        }

        public int CargarProductosDemo()
        {
            var productosDemo = new List<Producto>
            {
                new Producto { CodigoBarra = "780123456781", Nombre = "Leche Entera 1L", PrecioUnitario = 1150, Stock = 40, Estado = true },
                new Producto { CodigoBarra = "780123456782", Nombre = "Queso Gauda 250g", PrecioUnitario = 2490, Stock = 25, Estado = true },
                new Producto { CodigoBarra = "780123456783", Nombre = "Jamón Pierna 200g", PrecioUnitario = 1990, Stock = 20, Estado = true },
                new Producto { CodigoBarra = "780123456784", Nombre = "Bebida Sprite 1.5L", PrecioUnitario = 1500, Stock = 30, Estado = true },
                new Producto { CodigoBarra = "780123456785", Nombre = "Galletas Tritón 126g", PrecioUnitario = 850, Stock = 50, Estado = true },
                new Producto { CodigoBarra = "780123456786", Nombre = "Café Nescafé 170g", PrecioUnitario = 4200, Stock = 15, Estado = true },
                new Producto { CodigoBarra = "780123456787", Nombre = "Azúcar Blanca 1kg", PrecioUnitario = 1290, Stock = 60, Estado = true },
                new Producto { CodigoBarra = "780123456788", Nombre = "Aceite Vegetal 900ml", PrecioUnitario = 2190, Stock = 35, Estado = true },
                new Producto { CodigoBarra = "780123456789", Nombre = "Papas Chips 130g", PrecioUnitario = 1490, Stock = 45, Estado = true },
                new Producto { CodigoBarra = "780123456790", Nombre = "Yogurt Frutilla 125g", PrecioUnitario = 450, Stock = 80, Estado = true }
            };

            int agregados = 0;
            foreach (var p in productosDemo)
            {
                bool existe = _db.Productos.Any(x => x.CodigoBarra == p.CodigoBarra || x.Nombre == p.Nombre);
                if (!existe)
                {
                    _db.Productos.Add(p);
                    agregados++;
                }
            }

            if (agregados > 0)
            {
                _db.SaveChanges();
            }

            return agregados;
        }
    }
}