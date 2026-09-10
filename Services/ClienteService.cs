using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Helpers;

namespace SISTEMAACTUALIZADO.Services
{
    public class InfoCrediticiaClienteDTO
    {
        public Cliente Cliente { get; set; } = null!;
        public decimal DeudaTotal { get; set; }
        public decimal CupoDisponible { get; set; }
        public int FacturasVencidas { get; set; }
    }

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
            string rutLimpio = RutHelper.Limpiar(rut).ToLower();

            return db.Clientes.AsNoTracking().FirstOrDefault(c => 
                c.Rut.ToLower() == rutLimpio && c.Estado
            );
        }

        public InfoCrediticiaClienteDTO? ObtenerInfoCrediticiaPorRut(string rut)
        {
            string rutLimpio = RutHelper.Limpiar(rut);
            if (string.IsNullOrWhiteSpace(rutLimpio)) return null;

            using var db = new AppDbContext();
            var cli = db.Clientes.AsNoTracking().FirstOrDefault(c => c.Rut == rutLimpio && c.Estado);
            if (cli == null) return null;

            var facturasCliente = db.CuentasPorCobrar
                .AsNoTracking()
                .Where(c => c.IdCliente == cli.IdCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA"))
                .ToList();

            decimal deudaTotal = facturasCliente.Sum(c => c.SaldoPendiente);
            decimal cupoDisp = Math.Max(0, cli.CupoCredito - deudaTotal);
            int facturasVencidas = facturasCliente.Count(c => c.FechaVencimiento < DateTime.Today);

            return new InfoCrediticiaClienteDTO
            {
                Cliente = cli,
                DeudaTotal = deudaTotal,
                CupoDisponible = cupoDisp,
                FacturasVencidas = facturasVencidas
            };
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

        public void GuardarCliente(Cliente cliente, bool esNuevo, string usuarioResponsable = "ADMIN")
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
                    int diasAnt = existente.DiasCreditoHabiles;
                    decimal cupoAnt = existente.CupoCredito;
                    string estadoAnt = existente.EstadoCrediticio ?? "ACTIVO";

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
                    existente.DiasCreditoHabiles = cliente.DiasCreditoHabiles;
                    existente.CupoCredito = cliente.CupoCredito;
                    existente.ListaPrecioDefecto = cliente.ListaPrecioDefecto;
                    existente.CategoriaCliente = cliente.CategoriaCliente;
                    existente.PermiteCredito = cliente.PermiteCredito;
                    existente.ModalidadPago = cliente.ModalidadPago;
                    existente.EstadoCrediticio = cliente.EstadoCrediticio;
                    existente.Estado = cliente.Estado;

                    if (diasAnt != cliente.DiasCreditoHabiles || cupoAnt != cliente.CupoCredito)
                    {
                        db.HistorialCondicionesCredito.Add(new HistorialCondicionesCredito
                        {
                            IdCliente = existente.IdCliente,
                            DiasCreditoAnterior = diasAnt,
                            DiasCreditoNuevo = cliente.DiasCreditoHabiles,
                            CupoAnterior = cupoAnt,
                            CupoNuevo = cliente.CupoCredito,
                            EstadoAnterior = estadoAnt,
                            EstadoNuevo = existente.EstadoCrediticio,
                            Motivo = "Modificación de condiciones crediticias desde ficha de cliente",
                            FechaCambio = DateTime.Now,
                            UsuarioResponsable = usuarioResponsable
                        });
                    }
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

        public void EliminarCliente(int idCliente)
        {
            using var db = new AppDbContext();
            var cliente = db.Clientes.Find(idCliente);
            if (cliente != null)
            {
                cliente.Estado = false;
                db.SaveChanges();
            }
        }
    }
}