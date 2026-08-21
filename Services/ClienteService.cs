using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ClienteService
    {
        public List<Cliente> ObtenerClientes(string filtro = "")
        {
            using var db = new AppDbContext();
            string q = filtro.Trim().ToLower();

            var query = db.Clientes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c => 
                    (!string.IsNullOrEmpty(c.RazonSocial) && c.RazonSocial.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Rut) && c.Rut.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Giro) && c.Giro.ToLower().Contains(q))
                );
            }

            return query.OrderBy(c => c.RazonSocial).ToList();
        }

        public Cliente? BuscarPorRut(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut)) return null;

            using var db = new AppDbContext();
            string rutLimpio = rut.Replace(".", "").Replace("-", "").Trim().ToLower();

            return db.Clientes.AsNoTracking().FirstOrDefault(c => 
                c.Rut.Replace(".", "").Replace("-", "").Trim().ToLower() == rutLimpio
            );
        }

        public List<Cliente> BuscarClientesPredictivo(string busqueda, int limite = 5)
        {
            if (string.IsNullOrWhiteSpace(busqueda)) return new List<Cliente>();

            using var db = new AppDbContext();
            string q = busqueda.Trim().ToLower();

            return db.Clientes.AsNoTracking()
                .Where(c => c.Estado && (
                    (!string.IsNullOrEmpty(c.RazonSocial) && c.RazonSocial.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Rut) && c.Rut.ToLower().Contains(q))
                ))
                .OrderBy(c => c.RazonSocial)
                .Take(limite)
                .ToList();
        }

        public void GuardarCliente(Cliente cliente, bool esNuevo)
        {
            using var db = new AppDbContext();

            if (esNuevo)
            {
                db.Clientes.Add(cliente);
            }
            else
            {
                var existente = db.Clientes.Find(cliente.IdCliente);
                if (existente != null)
                {
                    existente.Rut = cliente.Rut;
                    existente.RazonSocial = cliente.RazonSocial;
                    existente.Giro = cliente.Giro;
                    existente.Direccion = cliente.Direccion;
                    existente.Comuna = cliente.Comuna;
                    existente.Ciudad = cliente.Ciudad;
                    existente.Telefono = cliente.Telefono;
                    existente.Email = cliente.Email;
                    existente.FormaPago = cliente.FormaPago;
                    existente.DiasCredito = cliente.DiasCredito;
                    existente.CupoCredito = cliente.CupoCredito;
                    existente.ListaPrecioDefecto = cliente.ListaPrecioDefecto;
                    existente.CategoriaCliente = cliente.CategoriaCliente;
                    existente.Estado = cliente.Estado;
                }
            }

            db.SaveChanges();
        }

        public void CambiarEstado(int idCliente)
        {
            using var db = new AppDbContext();
            var cliente = db.Clientes.Find(idCliente);
            if (cliente != null)
            {
                cliente.Estado = !cliente.Estado;
                db.SaveChanges();
            }
        }

        public List<dynamic> ObtenerUltimasComprasPorRut(string? rut, int cantidad = 2)
        {
            if (string.IsNullOrWhiteSpace(rut)) return new List<dynamic>();

            using var db = new AppDbContext();
            string rutLimpio = rut.Replace(".", "").Replace("-", "").Trim().ToLower();

            // Consulta desacoplada para leer últimas ventas asociadas al RUT
            return new List<dynamic>();
        }
    }
}