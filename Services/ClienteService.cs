using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ClienteService
    {
        private readonly AppDbContext _db = new AppDbContext();

        public List<Cliente> ObtenerClientes(string filtro = "")
        {
            var query = _db.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(c => (c.Nombre != null && c.Nombre.Contains(filtro)) ||
                                         (c.Rut != null && c.Rut.Contains(filtro)) ||
                                         (c.Email != null && c.Email.Contains(filtro)));
            }

            return query.OrderBy(c => c.Nombre).ToList();
        }

        // BÚSQUEDA EXACTA O LIMPIA POR RUT (Sin errores de traducción EF Core)
        public Cliente? BuscarPorRut(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut)) return null;

            string rutLimpio = rut.Replace(".", "").Replace("-", "").Trim().ToLower();

            // Evaluamos en memoria para permitir formateo de puntos y guiones
            return _db.Clientes.AsEnumerable().FirstOrDefault(c => 
                c.Rut != null && c.Rut.Replace(".", "").Replace("-", "").Trim().ToLower() == rutLimpio);
        }

        // BÚSQUEDA INTELIGENTE EN VIVO POR COINCIDENCIA DE RUT O NOMBRE
        public List<Cliente> BuscarClientesPredictivo(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Cliente>();

            string queryLimpia = query.Replace(".", "").Replace("-", "").Trim().ToLower();

            return _db.Clientes.AsEnumerable()
                .Where(c => (c.Rut != null && c.Rut.Replace(".", "").Replace("-", "").ToLower().Contains(queryLimpia)) ||
                            (c.Nombre != null && c.Nombre.ToLower().Contains(queryLimpia)))
                .Take(5)
                .ToList();
        }

        public void GuardarCliente(Cliente cliente, bool esNuevo)
        {
            if (esNuevo)
            {
                _db.Clientes.Add(cliente);
            }
            _db.SaveChanges();
        }

        public void CambiarEstado(int clienteId)
        {
            var cliente = _db.Clientes.FirstOrDefault(c => c.ClienteID == clienteId);
            if (cliente != null)
            {
                cliente.Estado = !cliente.Estado;
                _db.SaveChanges();
            }
        }

        public List<TVE2607> ObtenerUltimasComprasPorRut(string rut, int limite = 2)
        {
            if (string.IsNullOrWhiteSpace(rut)) return new List<TVE2607>();

            return _db.TVE2607
                .Where(v => v.RuT == rut)
                .OrderByDescending(v => v.FecDoc)
                .Take(limite)
                .ToList();
        }
    }
}