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
            var query = db.Productos.AsNoTracking().Where(p => p.Estado).AsQueryable();

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
                .AsNoTracking()
                .Where(p => p.Estado && !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public List<string> ObtenerFamiliasPorCategoria(string categoria)
        {
            using var db = new AppDbContext();
            var query = db.Productos.AsNoTracking().Where(p => p.Estado);

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

        public List<string> ObtenerTodasLasFamilias()
        {
            using var db = new AppDbContext();
            return db.Productos
                .AsNoTracking()
                .Where(p => p.Estado && !string.IsNullOrEmpty(p.NFamilia))
                .Select(p => p.NFamilia!)
                .Distinct()
                .OrderBy(f => f)
                .ToList();
        }

        public Producto GuardarProducto(Producto producto, List<TramoEscalaDTO> tramos, bool esModoEscala, int cantidadFacturaInicial = 0)
        {
            using var db = new AppDbContext();

            if (producto.ProductoID == 0)
            {
                if (cantidadFacturaInicial > 0)
                {
                    producto.Stock = cantidadFacturaInicial;
                }
                db.Productos.Add(producto);
                db.SaveChanges();

                GuardarReglasEscalaBD(db, producto.ProductoID, producto.Nombre, tramos, esModoEscala);
                return producto;
            }
            else
            {
                var prodBd = db.Productos.Find(producto.ProductoID);
                if (prodBd != null)
                {
                    prodBd.CodigoBarra = producto.CodigoBarra;
                    prodBd.Nombre = producto.Nombre;
                    prodBd.Categoria = producto.Categoria;
                    prodBd.NFamilia = producto.NFamilia;
                    prodBd.PrecioCosto = producto.PrecioCosto;
                    prodBd.ListaDefectoPOS = producto.ListaDefectoPOS;
                    prodBd.PrecioUnitario = producto.PrecioUnitario;
                    prodBd.Stock = producto.Stock;
                    prodBd.StockMinimo = producto.StockMinimo;
                    prodBd.ImagenPath = producto.ImagenPath;
                    prodBd.FchUpd = DateTime.Now;
                    prodBd.Sincro = 0;

                    GuardarReglasEscalaBD(db, prodBd.ProductoID, prodBd.Nombre, tramos, esModoEscala);
                    db.SaveChanges();
                    return prodBd;
                }
                return producto;
            }
        }

        private void GuardarReglasEscalaBD(AppDbContext db, int productoId, string nombreProducto, List<TramoEscalaDTO> tramos, bool esModoEscala)
        {
            var existentes = db.PreciosQ.Where(pq => pq.IdProducto == productoId).ToList();
            int bloqueoEstado = esModoEscala ? 0 : 1;

            if (tramos.Count >= 2)
            {
                if (existentes.Count > 0)
                {
                    db.PreciosQ.RemoveRange(existentes);
                }

                var prod = db.Productos.Find(productoId);

                foreach (var f in tramos)
                {
                    decimal precioValor = prod != null ? ObtenerPrecioPorNumeroLista(prod, f.NumeroLista) : 0m;

                    db.PreciosQ.Add(new PrecioQ
                    {
                        IdProducto = productoId,
                        NProducto = nombreProducto,
                        Qini = f.Desde,
                        Qfin = f.Hasta,
                        NPrecio = precioValor,
                        IdPrecio = f.NumeroLista.ToString(),
                        Bloqueo = bloqueoEstado,
                        FchMod = DateTime.Now,
                        HoraMod = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
                db.SaveChanges();
            }
        }

        public List<PrecioQ> ObtenerReglasEscala(int productoId)
        {
            using var db = new AppDbContext();
            return db.PreciosQ
                .AsNoTracking()
                .Where(pq => pq.IdProducto == productoId)
                .OrderBy(pq => pq.Qini)
                .ToList();
        }

        public void EliminarProducto(int productoId)
        {
            using var db = new AppDbContext();
            var prod = db.Productos.Find(productoId);
            if (prod != null)
            {
                prod.Estado = false;
                db.SaveChanges();
            }
        }

        public int AplicarMargenesYRecalcularPrecios(decimal[] nuevosMargenes, decimal[] margenesOriginales, bool redondear, string? categoriaFiltro)
        {
            using var db = new AppDbContext();

            for (int i = 0; i < 10; i++)
            {
                int nroLista = i + 1;
                decimal nuevoPorcentaje = nuevosMargenes[i];

                if (nuevoPorcentaje != margenesOriginales[i])
                {
                    var config = db.ConfiguracionMargenes.FirstOrDefault(m => m.NumeroLista == nroLista);
                    if (config != null)
                    {
                        config.PorcentajeMargen = nuevoPorcentaje;
                        config.UltimaModificacion = DateTime.Now;
                    }
                }
            }
            db.SaveChanges();

            IQueryable<Producto> query = db.Productos.Where(p => p.Estado);

            if (!string.IsNullOrWhiteSpace(categoriaFiltro))
            {
                query = query.Where(p => p.Categoria == categoriaFiltro);
            }

            var productos = query.ToList();

            foreach (var prod in productos)
            {
                if (prod.PrecioCosto <= 0) continue;
                decimal costo = prod.PrecioCosto;

                prod.MargenGanancia = nuevosMargenes[0];
                prod.PrecioUnitario = CalcularPrecioVenta(costo, nuevosMargenes[0], redondear);
                prod.Precio2 = CalcularPrecioVenta(costo, nuevosMargenes[1], redondear);
                prod.Precio3 = CalcularPrecioVenta(costo, nuevosMargenes[2], redondear);
                prod.Precio4 = CalcularPrecioVenta(costo, nuevosMargenes[3], redondear);
                prod.Precio5 = CalcularPrecioVenta(costo, nuevosMargenes[4], redondear);
                prod.Precio6 = CalcularPrecioVenta(costo, nuevosMargenes[5], redondear);
                prod.Precio7 = CalcularPrecioVenta(costo, nuevosMargenes[6], redondear);
                prod.Precio8 = CalcularPrecioVenta(costo, nuevosMargenes[7], redondear);
                prod.Precio9 = CalcularPrecioVenta(costo, nuevosMargenes[8], redondear);
                prod.Precio10 = CalcularPrecioVenta(costo, nuevosMargenes[9], redondear);

                prod.FchUpd = DateTime.Now;
                prod.Sincro = 0;
            }

            db.SaveChanges();
            return productos.Count;
        }

        public decimal ObtenerPrecioSegunCantidad(int productoId, int cantidad, decimal precioBase)
        {
            using var db = new AppDbContext();
            var escala = db.PreciosQ
                .AsNoTracking()
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

            if (clienteId > 0)
            {
                var especial = db.PreciosEspecialesClientes
                    .AsNoTracking()
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

            decimal precioCliente = ObtenerPrecioPorNumeroLista(prod, listaCliente);

            var reglaTramo = db.PreciosQ
                .AsNoTracking()
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

            var query = from pe in db.PreciosEspecialesClientes.AsNoTracking()
                        join pr in db.Productos.AsNoTracking() on pe.ProductoId equals pr.ProductoID
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

        public List<PrecioEspecialItemDTO> ObtenerPreciosEspecialesCliente(int clienteId)
        {
            DateTime hoy = DateTime.Today;
            using var db = new AppDbContext();

            return (from pe in db.PreciosEspecialesClientes.AsNoTracking()
                    join pr in db.Productos.AsNoTracking() on pe.ProductoId equals pr.ProductoID
                    where pe.ClienteId == clienteId && pe.Estado
                    orderby pe.FechaFin descending
                    select new PrecioEspecialItemDTO
                    {
                        IdEspecial = pe.IdEspecial,
                        Producto = pr.Nombre,
                        CostoBase = pr.PrecioCosto,
                        PrecioEspecial = pe.PrecioEspecial,
                        Desde = pe.FechaInicio,
                        Hasta = pe.FechaFin,
                        EstadoVigencia = (pe.FechaInicio <= hoy && pe.FechaFin >= hoy) ? "🟢 VIGENTE" : (pe.FechaFin < hoy ? "🔴 EXPIRADO" : "🟡 PROGRAMADO")
                    }).Take(10).ToList();
        }

        public void GuardarPrecioEspecial(PrecioEspecialCliente precioEspecial)
        {
            using var db = new AppDbContext();
            db.PreciosEspecialesClientes.Add(precioEspecial);
            db.SaveChanges();
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

        public static decimal[] ObtenerMargenesConfigurados()
        {
            decimal[] margenes = new decimal[10];
            try
            {
                using var db = new AppDbContext();
                var listaMargenes = db.ConfiguracionMargenes.AsNoTracking().ToList();

                for (int i = 0; i < 10; i++)
                {
                    int nroLista = i + 1;
                    var item = listaMargenes.FirstOrDefault(m => m.NumeroLista == nroLista);
                    margenes[i] = item != null ? item.PorcentajeMargen : 0m;
                }
            }
            catch { }

            return margenes;
        }

        public static decimal CalcularPrecioVenta(decimal costoNeto, decimal margenPorcentaje, bool redondear = true)
        {
            if (costoNeto <= 0 || margenPorcentaje <= 0) return 0m;
            decimal neto = costoNeto * (1m + (margenPorcentaje / 100m));
            decimal bruto = neto * 1.19m;
            return redondear ? Math.Round(bruto / 10m, MidpointRounding.AwayFromZero) * 10m : Math.Round(bruto, 0);
        }

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

    public class TramoEscalaDTO
    {
        public int NumeroTramo { get; set; }
        public decimal Desde { get; set; }
        public decimal Hasta { get; set; }
        public int NumeroLista { get; set; }
    }
}