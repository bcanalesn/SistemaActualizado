using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Helpers;

namespace SISTEMAACTUALIZADO.Services
{
    public class VentaService
    {
        public int GenerarTicketVenta(List<DetalleCarrito> carrito, string vendedor, string clienteNombre, string clienteRut)
        {
            using (var db = new AppDbContext())
            {
                int ultimoNroInT = db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;

                int nroTicketAtencion = (int)(DateTime.Now.Ticks % 1000000);

                // Buscar datos del cliente si no es consumidor final
                string rutLimpio = RutHelper.Limpiar(clienteRut ?? "");
                var clienteDb = db.Clientes.FirstOrDefault(c => !string.IsNullOrEmpty(rutLimpio) && (c.Rut == rutLimpio || c.Rut == clienteRut));

                var nuevaVenta = new TVE2607
                {
                    idLocal = 1,
                    nmbLocal = "Local Principal",
                    iddocDTE = 0,
                    Documento = "Ticket de Atención",
                    nroDTE = nroTicketAtencion,
                    nroInT = siguienteNroInT,
                    FecDoc = DateTime.Now,
                    SubTotal = carrito.Sum(c => c.Subtotal),
                    Descuento = 0,
                    Neto = Math.Round(carrito.Sum(c => c.Subtotal) / 1.19m, 0),
                    IvA = carrito.Sum(c => c.Subtotal) - Math.Round(carrito.Sum(c => c.Subtotal) / 1.19m, 0),
                    Total = carrito.Sum(c => c.Subtotal),
                    UserDTE = vendedor,
                    Vendedor = vendedor,
                    Idcliente = clienteDb?.IdCliente ?? 0,
                    RuT = clienteDb != null ? RutHelper.Formatear(clienteDb.Rut) : "",
                    RazonSocial = clienteDb?.RazonSocial ?? "Consumidor Final",
                    Giro = clienteDb?.Giro ?? "",
                    Direccion = clienteDb?.Direccion ?? "",
                    nComuna = clienteDb?.Comuna ?? "",
                    nCiudad = clienteDb?.Ciudad ?? "",
                    Fono1 = clienteDb?.Telefono ?? "",
                    email = clienteDb?.Email ?? "",
                    status = "Pendiente"
                };

                db.TVE2607.Add(nuevaVenta);
                db.SaveChanges();

                foreach (var item in carrito)
                {
                    var detalle = new TVD2607
                    {
                        idTve = nuevaVenta.idTve,
                        idLocal = 1,
                        iddocDTE = 0,
                        Documento = "Ticket de Atención",
                        NroDTE = nroTicketAtencion,
                        NroInT = siguienteNroInT,
                        FecMoV = DateTime.Now,
                        HoraMoV = DateTime.Now.ToString("HH:mm:ss"),
                        IdProducto = item.ProductoID,
                        NmbProducto = item.Nombre,
                        Cantidad = item.Cantidad,
                        Precio = item.PrecioUnitario,
                        SubTotal = item.Subtotal,
                        nmbVendedor = vendedor,
                        Unidad = "UN"
                    };

                    db.TVD2607.Add(detalle);

                    var prodBD = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prodBD != null)
                    {
                        prodBD.Stock -= item.Cantidad;
                        if (prodBD.Stock < 0) prodBD.Stock = 0;
                    }
                }

                db.SaveChanges();
                return nroTicketAtencion;
            }
        }
    }
}