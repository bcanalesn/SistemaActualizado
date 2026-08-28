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

        public decimal ObtenerPrecioProductoConCliente(Producto prod, int listaCliente, int cantidad, int clienteId = 0)
        {
            DateTime hoy = DateTime.Today;

            using var db = new AppDbContext();

            // 1. EVALUAR PRECIO ESPECIAL TEMPORAL VIGENTE PARA EL CLIENTE
            if (clienteId > 0)
            {
                var especial = db.PreciosEspecialesClientes
                    .Where(p => p.ClienteId == clienteId && 
                                p.ProductoId == prod.ProductoID && 
                                p.Estado && 
                                p.FechaInicio <= hoy && 
                                p.FechaFin >= hoy)
                    .OrderByDescending(p => p.IdEspecial)
                    .FirstOrDefault();

                if (especial != null && especial.PrecioEspecial > 0)
                {
                    return especial.PrecioEspecial;
                }
            }

            // 2. OBTENER PRECIO SEGÚN LA LISTA PREFERENCIAL DEL CLIENTE
            decimal precioCliente = ObtenerPrecioPorNumeroLista(prod, listaCliente);

            // 3. EVALUAR SI CALIFICA A MEJOR TRAMO POR VOLUMEN (PreciosQ)
            var reglaTramo = db.PreciosQ
                .Where(pq => pq.IdProducto == prod.ProductoID && pq.Bloqueo == 0 && cantidad >= pq.Qini && cantidad <= pq.Qfin)
                .FirstOrDefault();

            if (reglaTramo != null)
            {
                int nroListaTramo = ObtenerIndiceDesdeIdPrecio(reglaTramo.IdPrecio);
                decimal precioTramo = ObtenerPrecioPorNumeroLista(prod, nroListaTramo);

                if (precioTramo > 0 && precioTramo < precioCliente)
                {
                    return precioTramo;
                }
            }

            return precioCliente;
        }

        public List<(string NombreProducto, decimal PrecioPactado, DateTime Inicio, DateTime Fin)> ObtenerPromocionesVigentesCliente(int clienteId)
        {
            if (clienteId <= 0) return new List<(string, decimal, DateTime, DateTime)>();

            DateTime hoy = DateTime.Today;
            using var db = new AppDbContext();

            var query = from pe in db.PreciosEspecialesClientes
                        join pr in db.Productos on pe.ProductoId equals pr.ProductoID
                        where pe.ClienteId == clienteId && pe.Estado && pe.FechaInicio <= hoy && pe.FechaFin >= hoy
                        select new
                        {
                            pr.Nombre,
                            pe.PrecioEspecial,
                            pe.FechaInicio,
                            pe.FechaFin
                        };

            return query.ToList().Select(x => (x.Nombre, x.PrecioEspecial, x.FechaInicio, x.FechaFin)).ToList();
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

        // Obtiene los márgenes globales vigentes configurados en el sistema
        public static decimal[] ObtenerMargenesConfigurados()
        {
            try
            {
                using var db = new AppDbContext();
                var prod = db.Productos.FirstOrDefault(p => p.Estado && p.PrecioCosto > 0 && p.PrecioUnitario > 0);
                if (prod != null)
                {
                    decimal costo = prod.PrecioCosto;
                    decimal[] precios = new decimal[] { prod.PrecioUnitario, prod.Precio2, prod.Precio3, prod.Precio4, prod.Precio5, prod.Precio6, prod.Precio7, prod.Precio8, prod.Precio9, prod.Precio10 };
                    decimal[] margenes = new decimal[10];

                    for (int i = 0; i < 10; i++)
                    {
                        if (precios[i] > 0)
                        {
                            decimal neto = precios[i] / 1.19m;
                            decimal margenCalc = ((neto / costo) - 1m) * 100m;
                            margenes[i] = Math.Round(margenCalc, 1);
                        }
                    }
                    return margenes;
                }
            }
            catch { }

            return new decimal[] { 60m, 50m, 18m, 12m, 5m, 0m, 0m, 0m, 0m, 0m };
        }

        // Calcula el precio bruto final con IVA y redondeo a la decena
        public static decimal CalcularPrecioVenta(decimal costoNeto, decimal margenPorcentaje, bool redondear = true)
        {
            if (costoNeto <= 0 || margenPorcentaje <= 0) return 0m;
            decimal neto = costoNeto * (1m + (margenPorcentaje / 100m));
            decimal bruto = neto * 1.19m;
            return redondear ? Math.Round(bruto / 10m, MidpointRounding.AwayFromZero) * 10m : Math.Round(bruto, 0);
        }

        // Aplica el nuevo costo y recalcula todas las listas activas (> 0%)
        public static void AplicarNuevoCostoYRecalcularListas(Producto prod, decimal nuevoCostoNeto, decimal[]? margenes = null)
        {
            if (nuevoCostoNeto <= 0) return;
            margenes ??= ObtenerMargenesConfigurados();

            prod.PrecioCosto = nuevoCostoNeto;
            if (margenes[0] > 0) prod.PrecioUnitario = CalcularPrecioVenta(nuevoCostoNeto, margenes[0]);
            prod.Precio2 = margenes[1] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[1]) : 0m;
            prod.Precio3 = margenes[2] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[2]) : 0m;
            prod.Precio4 = margenes[3] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[3]) : 0m;
            prod.Precio5 = margenes[4] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[4]) : 0m;
            prod.Precio6 = margenes[5] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[5]) : 0m;
            prod.Precio7 = margenes[6] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[6]) : 0m;
            prod.Precio8 = margenes[7] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[7]) : 0m;
            prod.Precio9 = margenes[8] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[8]) : 0m;
            prod.Precio10 = margenes[9] > 0 ? CalcularPrecioVenta(nuevoCostoNeto, margenes[9]) : 0m;
            prod.FchUpd = DateTime.Now;
            prod.Sincro = 0;
        }
    }
}