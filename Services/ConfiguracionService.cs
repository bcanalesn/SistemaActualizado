using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ConfiguracionService
    {
        // =========================================================================
        // 1. DATOS DE EMPRESA
        // =========================================================================

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
        // 2. PREFERENCIAS DEL CATÁLOGO POS
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

        /// <summary>
        /// Aplica el criterio de ordenamiento activo a una colección de productos.
        /// </summary>
        public IEnumerable<Producto> AplicarCriterioOrdenPOS(IEnumerable<Producto> productos, string? modoForzado = null)
        {
            string modo = modoForzado ?? ObtenerModoOrdenCatalogo();

            // OPCIÓN 2: PERSONALIZADO POR EL LOCAL (OrdenPOS: 1, 2, 3...)
            if (modo == "PERSONALIZADO")
            {
                return productos
                    .OrderBy(p => p.OrdenPOS > 0 ? p.OrdenPOS : int.MaxValue)
                    .ThenBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase);
            }
            // OPCIÓN 3: LOS MÁS VENDIDOS (Frecuencia en boletas y facturas emitidas)
            else if (modo == "MAS_VENDIDOS")
            {
                using var db = new AppDbContext();

                // Exactamente la misma consulta SQL de tu Workbench:
                // SELECT IdProducto, COUNT(idTvd) FROM tvd2607 INNER JOIN tve2607 WHERE status = 'Emitido' GROUP BY IdProducto
                var popularidad = (from d in db.TVD2607.AsNoTracking()
                                   join v in db.TVE2607.AsNoTracking() on d.idTve equals v.idTve
                                   where v.status == "Emitido"
                                   group d by d.IdProducto into g
                                   select new
                                   {
                                       ProductoID = g.Key,
                                       Frecuencia = g.Count() // ✔ COUNT: Cuenta transacciones, NO suma gramos
                                   })
                                   .ToDictionary(k => k.ProductoID, v => v.Frecuencia);

                return productos
                    .OrderByDescending(p => popularidad.ContainsKey(p.ProductoID) ? popularidad[p.ProductoID] : 0)
                    .ThenBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase);
            }

            // OPCIÓN 1: ALFABÉTICO (A - Z por Nombre de Producto)
            return productos
                .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase);
        }

        // =========================================================================
        // 3. GESTIÓN DE CATEGORÍAS (EN BASE DE DATOS)
        // =========================================================================

        public List<PosCategoria> ObtenerCategoriasPOS()
        {
            using var db = new AppDbContext();
            return db.PosCategorias
                .AsNoTracking()
                .Include(c => c.Familias.Where(f => f.Activo))
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToList();
        }

        public void GuardarOrdenCategorias(List<int> categoriaIds)
        {
            using var db = new AppDbContext();
            var cats = db.PosCategorias.Where(c => categoriaIds.Contains(c.Id)).ToList();
            for (int i = 0; i < categoriaIds.Count; i++)
            {
                var cat = cats.FirstOrDefault(c => c.Id == categoriaIds[i]);
                if (cat != null) cat.Orden = i + 1;
            }
            db.SaveChanges();
        }

        public void RenombrarCategoria(int idCategoria, string nuevoNombre)
        {
            using var db = new AppDbContext();
            var cat = db.PosCategorias.Find(idCategoria);
            if (cat == null) return;

            string viejoNombre = cat.Nombre;
            cat.Nombre = nuevoNombre.Trim();

            // Cascada en la tabla productos para mantener la consistencia
            var prods = db.Productos.Where(p => p.Categoria == viejoNombre).ToList();
            foreach (var p in prods)
            {
                p.Categoria = cat.Nombre;
                p.FchUpd = DateTime.Now;
            }

            db.SaveChanges();
        }

        public void ActualizarColorCategoria(int idCategoria, string fondoHex, string textoHex, string bordeHex)
        {
            using var db = new AppDbContext();
            var cat = db.PosCategorias.Find(idCategoria);
            if (cat != null)
            {
                cat.FondoHex = fondoHex;
                cat.TextoHex = textoHex;
                cat.BordeHex = bordeHex;
                db.SaveChanges();
            }
        }

        public void EliminarCategoria(int idCategoria, string? categoriaReemplazo = "General")
        {
            using var db = new AppDbContext();
            var cat = db.PosCategorias.Find(idCategoria);
            if (cat == null) return;

            cat.Activo = false;

            // Reasignar productos huérfanos a la categoría de reemplazo
            var prods = db.Productos.Where(p => p.Categoria == cat.Nombre).ToList();
            foreach (var p in prods)
            {
                p.Categoria = categoriaReemplazo ?? "General";
                p.FchUpd = DateTime.Now;
            }

            db.SaveChanges();
        }

        // =========================================================================
        // 4. GESTIÓN DE SUBFAMILIAS (EN BASE DE DATOS)
        // =========================================================================

        public void CrearFamilia(int categoriaId, string nombreFamilia)
        {
            using var db = new AppDbContext();
            int maxOrden = db.PosFamilias
                .Where(f => f.CategoriaId == categoriaId && f.Activo)
                .Max(f => (int?)f.Orden) ?? 0;

            db.PosFamilias.Add(new PosFamilia
            {
                CategoriaId = categoriaId,
                Nombre = nombreFamilia.Trim(),
                Orden = maxOrden + 1,
                Activo = true
            });
            db.SaveChanges();
        }

        public void RenombrarFamilia(int familiaId, string nuevoNombre)
        {
            using var db = new AppDbContext();
            var fam = db.PosFamilias.Find(familiaId);
            if (fam == null) return;

            string viejoNombre = fam.Nombre;
            fam.Nombre = nuevoNombre.Trim();

            var cat = db.PosCategorias.Find(fam.CategoriaId);
            if (cat != null)
            {
                var prods = db.Productos
                    .Where(p => p.Categoria == cat.Nombre && p.NFamilia == viejoNombre)
                    .ToList();
                foreach (var p in prods)
                {
                    p.NFamilia = fam.Nombre;
                    p.FchUpd = DateTime.Now;
                }
            }

            db.SaveChanges();
        }

        public void GuardarOrdenFamilias(List<int> familiaIds)
        {
            using var db = new AppDbContext();
            var fams = db.PosFamilias.Where(f => familiaIds.Contains(f.Id)).ToList();
            for (int i = 0; i < familiaIds.Count; i++)
            {
                var fam = fams.FirstOrDefault(f => f.Id == familiaIds[i]);
                if (fam != null) fam.Orden = i + 1;
            }
            db.SaveChanges();
        }

        public void EliminarFamilia(int familiaId, string? familiaReemplazo = "General")
        {
            using var db = new AppDbContext();
            var fam = db.PosFamilias.Find(familiaId);
            if (fam == null) return;

            fam.Activo = false;

            var cat = db.PosCategorias.Find(fam.CategoriaId);
            if (cat != null)
            {
                var prods = db.Productos
                    .Where(p => p.Categoria == cat.Nombre && p.NFamilia == fam.Nombre)
                    .ToList();
                foreach (var p in prods)
                {
                    p.NFamilia = familiaReemplazo ?? "General";
                    p.FchUpd = DateTime.Now;
                }
            }

            db.SaveChanges();
        }

        // =========================================================================
        // 5. ASIGNACIÓN Y MOVIMIENTO DE PRODUCTOS EN EL LIENZO
        // =========================================================================

        public void AsignarProductoACategoriaYFamilia(int productoId, string nuevaCategoria, string nuevaFamilia, int nuevoOrden)
        {
            using var db = new AppDbContext();
            var prod = db.Productos.Find(productoId);
            if (prod != null)
            {
                prod.Categoria = nuevaCategoria;
                if (!string.IsNullOrWhiteSpace(nuevaFamilia))
                {
                    prod.NFamilia = nuevaFamilia;
                }
                prod.OrdenPOS = nuevoOrden;
                prod.FchUpd = DateTime.Now;
                db.SaveChanges();
            }
        }
        /// <summary>
        /// Inserta o mueve un producto a una posición específica desplazando a los demás hacia la derecha.
        /// </summary>
        public void ReordenarEInsertarProducto(int productoId, string categoria, string familia, int posicionDestino)
        {
            using var db = new AppDbContext();

            // 1. Obtener los productos que pertenecen a este contexto
            var productosContexto = db.Productos
                .Where(p => p.Estado && p.Categoria == categoria && (string.IsNullOrEmpty(familia) || p.NFamilia == familia))
                .OrderBy(p => p.OrdenPOS > 0 ? p.OrdenPOS : int.MaxValue)
                .ThenBy(p => p.Nombre)
                .ToList();

            // 2. Extraer el producto a mover si ya existía en este contexto
            var target = db.Productos.Find(productoId);
            if (target == null) return;

            target.Categoria = categoria;
            if (!string.IsNullOrWhiteSpace(familia)) target.NFamilia = familia;

            productosContexto.RemoveAll(p => p.ProductoID == productoId);

            // 3. Ajustar índice (0-based) asegurando límites válidos
            int indexDestino = Math.Max(0, Math.Min(posicionDestino - 1, productosContexto.Count));
            productosContexto.Insert(indexDestino, target);

            // 4. Renumerar correlativamente del 1 al N sin duplicados ni huecos
            for (int i = 0; i < productosContexto.Count; i++)
            {
                productosContexto[i].OrdenPOS = i + 1;
                productosContexto[i].FchUpd = DateTime.Now;
            }

            db.SaveChanges();
        }
    }
}