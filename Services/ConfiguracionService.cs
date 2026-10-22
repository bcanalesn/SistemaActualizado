using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ConfiguracionService
    {
        public ConfiguracionEmpresa ObtenerDatosEmpresa()
        {
            using var db = new AppDbContext();
            return db.ConfiguracionEmpresa.AsNoTracking().FirstOrDefault(e => e.EmpresaID == 1) 
                   ?? new ConfiguracionEmpresa { EmpresaID = 1 };
        }

        public void GuardarDatosEmpresa(ConfiguracionEmpresa datos)
        {
            using var db = new AppDbContext();
            var emp = db.ConfiguracionEmpresa.FirstOrDefault(e => e.EmpresaID == 1);

            if (emp == null)
            {
                datos.EmpresaID = 1;
                db.ConfiguracionEmpresa.Add(datos);
            }
            else
            {
                emp.Rut = datos.Rut;
                emp.RazonSocial = datos.RazonSocial;
                emp.Giro = datos.Giro;
                emp.Direccion = datos.Direccion;
                emp.Comuna = datos.Comuna;
                emp.Ciudad = datos.Ciudad;
                emp.Telefono = datos.Telefono;
                emp.Email = datos.Email;
                emp.UnidadSII = datos.UnidadSII;
                emp.ResolucionSII = datos.ResolucionSII;
                emp.TextoPieTicket = datos.TextoPieTicket;
            }

            db.SaveChanges();
        }
        // =========================================================================
        // ORDEN Y PERSONALIZACIÓN DEL CATÁLOGO POS
        // =========================================================================

        public string ObtenerModoOrdenCatalogo()
        {
            using var db = new AppDbContext();
            var pref = db.PosPreferencias.AsNoTracking().FirstOrDefault(p => p.Id == 1);
            return pref?.ModoOrdenCatalogo ?? "CATEGORIA";
        }

        public void GuardarModoOrdenCatalogo(string modo)
        {
            using var db = new AppDbContext();
            var pref = db.PosPreferencias.FirstOrDefault(p => p.Id == 1);
            if (pref == null)
            {
                pref = new PosPreferencia { Id = 1, ModoOrdenCatalogo = modo };
                db.PosPreferencias.Add(pref);
            }
            else
            {
                pref.ModoOrdenCatalogo = modo;
                pref.FechaActualizacion = DateTime.Now;
            }
            db.SaveChanges();
        }

        public List<Producto> ObtenerProductosParaConfigurarOrden()
        {
            using var db = new AppDbContext();
            var lista = db.Productos
                .AsNoTracking()
                .Where(p => p.Estado)
                .OrderBy(p => p.OrdenPOS > 0 ? p.OrdenPOS : int.MaxValue)
                .ThenBy(p => p.Nombre)
                .ToList();

            // Asegurar que tengan números correlativos continuos de 1 a N
            for (int i = 0; i < lista.Count; i++)
            {
                lista[i].OrdenPOS = i + 1;
            }

            return lista;
        }

        public void GuardarOrdenPersonalizadoProductos(List<(int ProductoID, int NuevoOrden)> ordenes)
        {
            using var db = new AppDbContext();
            var ids = ordenes.Select(o => o.ProductoID).ToList();
            var productosBD = db.Productos.Where(p => ids.Contains(p.ProductoID)).ToList();

            foreach (var prod in productosBD)
            {
                var match = ordenes.FirstOrDefault(o => o.ProductoID == prod.ProductoID);
                if (match != default)
                {
                    prod.OrdenPOS = match.NuevoOrden;
                }
            }

            db.SaveChanges();
        }

        /// <summary>
        /// Aplica el criterio de ordenamiento activo a la lista de productos del POS.
        /// </summary>
        public IEnumerable<Producto> AplicarCriterioOrdenPOS(IEnumerable<Producto> productos, string? modoForzado = null)
        {
            string modo = modoForzado ?? ObtenerModoOrdenCatalogo();

            // OPCIÓN 2: PERSONALIZADO POR EL LOCAL (1, 2, 3...)
            if (modo == "PERSONALIZADO")
            {
                return productos
                    .OrderBy(p => p.OrdenPOS > 0 ? p.OrdenPOS : int.MaxValue)
                    .ThenBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase);
            }
            // OPCIÓN 3: LOS MÁS VENDIDOS (FRECUENCIA EN VENTAS EMITIDAS)
            else if (modo == "MAS_VENDIDOS")
            {
                using var db = new AppDbContext();

                var popularidad = db.TVD2607
                    .AsNoTracking()
                    .Where(d => d.VentaEncabezado != null && d.VentaEncabezado.status == "Emitido")
                    .GroupBy(d => d.IdProducto)
                    .Select(g => new
                    {
                        ProductoID = g.Key,
                        Frecuencia = g.Count()
                    })
                    .ToDictionary(k => k.ProductoID, v => v.Frecuencia);

                return productos
                    .OrderByDescending(p => popularidad.ContainsKey(p.ProductoID) ? popularidad[p.ProductoID] : 0)
                    .ThenBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase);
            }

            // OPCIÓN 1: ORDEN ALFABÉTICO TRADICIONAL (A - Z por Nombre de Producto)
            return productos
                .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}