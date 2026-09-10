using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class UsuarioService
    {
        public List<Usuario> ObtenerUsuarios(string filtro = "")
        {
            using var db = new AppDbContext();
            var query = db.Usuarios.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string q = filtro.Trim().ToLower();
                query = query.Where(u => u.NombreUsuario.ToLower().Contains(q) || u.NombreCompleto.ToLower().Contains(q));
            }

            return query.OrderBy(u => u.NombreUsuario).ToList();
        }

        public void GuardarUsuario(Usuario usuario, bool esNuevo)
        {
            using var db = new AppDbContext();

            if (esNuevo)
            {
                db.Usuarios.Add(usuario);
            }
            else
            {
                var existente = db.Usuarios.Find(usuario.UsuarioID);
                if (existente != null)
                {
                    existente.NombreUsuario = usuario.NombreUsuario;
                    existente.NombreCompleto = usuario.NombreCompleto;
                    existente.Clave = usuario.Clave;
                    existente.Rol = usuario.Rol;
                    existente.Estado = usuario.Estado;
                }
            }

            db.SaveChanges();
        }

        public void AlternarEstado(int usuarioId)
        {
            using var db = new AppDbContext();
            var u = db.Usuarios.Find(usuarioId);
            if (u != null)
            {
                u.Estado = !u.Estado;
                db.SaveChanges();
            }
        }
    }
}