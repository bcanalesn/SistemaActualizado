using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ProveedorService
    {
        public List<Proveedor> ObtenerProveedores(string filtro = "")
        {
            using var db = new AppDbContext();
            var query = db.Proveedores.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string q = filtro.Trim().ToLower();
                query = query.Where(p => p.RazonSocial.ToLower().Contains(q) || 
                                         p.Rut.ToLower().Contains(q) || 
                                         p.Giro.ToLower().Contains(q));
            }

            return query.OrderBy(p => p.RazonSocial).ToList();
        }

        public List<Proveedor> ObtenerProveedoresActivos(string filtro = "")
        {
            using var db = new AppDbContext();
            var query = db.Proveedores.AsNoTracking().Where(p => p.Estado).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string q = filtro.Trim().ToLower();
                query = query.Where(p => p.RazonSocial.ToLower().Contains(q) || 
                                         p.Rut.ToLower().Contains(q) || 
                                         p.Giro.ToLower().Contains(q));
            }

            return query.OrderBy(p => p.RazonSocial).ToList();
        }

        public Proveedor GuardarProveedor(Proveedor proveedor)
        {
            using var db = new AppDbContext();

            if (proveedor.ProveedorID == 0)
            {
                proveedor.Estado = true;
                db.Proveedores.Add(proveedor);
                db.SaveChanges();
                return proveedor;
            }
            else
            {
                var pBd = db.Proveedores.Find(proveedor.ProveedorID);
                if (pBd != null)
                {
                    pBd.Rut = proveedor.Rut;
                    pBd.RazonSocial = proveedor.RazonSocial;
                    pBd.Giro = proveedor.Giro;
                    pBd.Telefono = proveedor.Telefono;
                    pBd.Email = proveedor.Email;
                    pBd.Direccion = proveedor.Direccion;
                    pBd.Estado = proveedor.Estado;
                    db.SaveChanges();
                    return pBd;
                }
                return proveedor;
            }
        }

        public void AlternarEstado(int proveedorId)
        {
            using var db = new AppDbContext();
            var p = db.Proveedores.Find(proveedorId);
            if (p != null)
            {
                p.Estado = !p.Estado;
                db.SaveChanges();
            }
        }

        public int CargarProveedoresDemo()
        {
            using var db = new AppDbContext();
            var demos = new List<Proveedor>
            {
                new Proveedor { Rut = "76.888.999-1", RazonSocial = "DISTRIBUIDORA LÁCTEOS SUR S.A.", Giro = "DISTRIBUCION DE PRODUCTOS LACTEOS Y PERECIBLES", Telefono = "+56 9 8888 1111", Email = "ventas@lacteossur.cl", Direccion = "Av. Industrial #450, Santiago", Estado = true },
                new Proveedor { Rut = "96.543.210-5", RazonSocial = "COMERCIALIZADORA DE ABARROTES CENTRAL LTDA", Giro = "IMPORTADORA Y DISTRIBUIDORA DE ABARROTES", Telefono = "+56 2 2333 4444", Email = "contacto@abarrotescentral.cl", Direccion = "Calle El Roble #1200, Quilicura", Estado = true },
                new Proveedor { Rut = "77.111.222-3", RazonSocial = "EMBUTIDOS Y CECINAS DEL VALLE SPA", Giro = "ELABORACION Y DISTRIBUCION DE CECINAS Y CARNES", Telefono = "+56 9 7777 3333", Email = "pedidos@cecinasdelvalle.cl", Direccion = "Ruta 5 Sur Km 210, Talca", Estado = true }
            };

            int agregados = 0;
            foreach (var d in demos)
            {
                if (!db.Proveedores.Any(p => p.Rut == d.Rut))
                {
                    db.Proveedores.Add(d);
                    agregados++;
                }
            }

            if (agregados > 0)
            {
                db.SaveChanges();
            }

            return agregados;
        }
    }
}