using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class VentaService
    {
        private readonly AppDbContext _db = new AppDbContext();

        public int GenerarTicketVenta(List<DetalleCarrito> carro, string vendedorNombre, string clienteNombre, string clienteRut)
        {
            decimal total = carro.Sum(c => c.Subtotal);
            int nroTicket = (int)(DateTime.Now.Ticks % 1000000);

            TVE2607 tve = new TVE2607
            {
                idLocal = 1,
                nmbLocal = "Local Principal",
                iddocDTE = 0, // 0 = Ticket de Atención Pendiente
                Documento = "Ticket de Atención",
                nroDTE = nroTicket,
                FecDoc = DateTime.Now,
                SubTotal = Math.Round(total / 1.19m, 0),
                Neto = Math.Round(total / 1.19m, 0),
                IvA = total - Math.Round(total / 1.19m, 0),
                Total = total,
                UserDTE = vendedorNombre,
                Vendedor = vendedorNombre,
                RuT = clienteRut,
                RazonSocial = clienteNombre,
                status = "Pendiente"
            };

            _db.TVE2607.Add(tve);
            _db.SaveChanges();

            foreach (var item in carro)
            {
                TVD2607 tvd = new TVD2607
                {
                    idTve = tve.idTve,
                    idLocal = 1,
                    iddocDTE = 0,
                    Documento = "Ticket de Atención",
                    NroDTE = nroTicket,
                    FecMoV = DateTime.Now,
                    IdProducto = item.ProductoID,
                    NmbProducto = item.Nombre,
                    Cantidad = item.Cantidad,
                    Precio = item.PrecioUnitario,
                    SubTotal = item.Subtotal,
                    nmbVendedor = vendedorNombre
                };
                _db.TVD2607.Add(tvd);

                // Descuento automático de stock en BD
                var prodBD = _db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                if (prodBD != null)
                {
                    prodBD.Stock -= item.Cantidad;
                    if (prodBD.Stock < 0) prodBD.Stock = 0;
                }
            }

            _db.SaveChanges();
            return nroTicket;
        }
    }
}