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

    }
}