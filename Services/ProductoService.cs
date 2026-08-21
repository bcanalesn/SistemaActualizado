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
        public decimal ObtenerPrecioProductoConCliente(Producto prod, int listaCliente, int cantidad)
        {
            // 1. Obtener precio según la lista preferencial del cliente
            decimal precioCliente = ObtenerPrecioPorNumeroLista(prod, listaCliente);

            // 2. Si el producto tiene escala por cantidad (PreciosQ), verificar si califica a un mejor tramo
            using var db = new AppDbContext();
            var reglaTramo = db.PreciosQ
                .Where(pq => pq.IdProducto == prod.ProductoID && pq.Bloqueo == 0 && cantidad >= pq.Qini && cantidad <= pq.Qfin)
                .FirstOrDefault();

            if (reglaTramo != null)
            {
                int nroListaTramo = ObtenerIndiceDesdeIdPrecio(reglaTramo.IdPrecio);
                decimal precioTramo = ObtenerPrecioPorNumeroLista(prod, nroListaTramo);

                // Si el precio por tramo de cantidad es más conveniente que el del cliente, se aplica
                if (precioTramo > 0 && precioTramo < precioCliente)
                {
                    return precioTramo;
                }
            }

            return precioCliente;
        }

        public decimal ObtenerPrecioPorNumeroLista(Producto prod, int nroLista)
        {
            return nroLista switch
            {
                1 => prod.PrecioUnitario,
                2 => prod.Precio2 > 0 ? prod.Precio2 : prod.PrecioUnitario,
                3 => prod.Precio3 > 0 ? prod.Precio3 : prod.PrecioUnitario,
                4 => prod.Precio4 > 0 ? prod.Precio4 : prod.PrecioUnitario,
                5 => prod.Precio5 > 0 ? prod.Precio5 : prod.PrecioUnitario,
                6 => prod.Precio6 > 0 ? prod.Precio6 : prod.PrecioUnitario,
                7 => prod.Precio7 > 0 ? prod.Precio7 : prod.PrecioUnitario,
                8 => prod.Precio8 > 0 ? prod.Precio8 : prod.PrecioUnitario,
                9 => prod.Precio9 > 0 ? prod.Precio9 : prod.PrecioUnitario,
                10 => prod.Precio10 > 0 ? prod.Precio10 : prod.PrecioUnitario,
                _ => prod.PrecioUnitario
            };
        }

        private int ObtenerIndiceDesdeIdPrecio(string? idPrecio)
        {
            if (string.IsNullOrEmpty(idPrecio)) return 1;
            string clean = idPrecio.Trim().ToLower().Replace("precio", "").Replace("d", "1");
            if (int.TryParse(clean, out int idx) && idx >= 1 && idx <= 10)
                return idx;
            return 1;
        }

    }
}