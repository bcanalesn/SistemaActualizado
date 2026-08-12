using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class VentaService
    {
        public int GenerarTicketVenta(List<DetalleCarrito> carrito, string vendedor, string clienteNombre, string clienteRut)
        {
            using (var db = new AppDbContext())
            {
                // 1. Obtener el último nroInT registrado y sumar 1
                int ultimoNroInT = db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;

                int nroTicketAtencion = (int)(DateTime.Now.Ticks % 1000000);

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
                    RuT = clienteRut,
                    RazonSocial = clienteNombre,
                    status = "Pendiente"
                };

                db.TVE2607.Add(nuevaVenta);
                db.SaveChanges(); // Guarda encabezado

                // 2. Guardar detalle Y DESCONTAR STOCK PERMANENTE EN BD
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

                    // --- AQUÍ ESTABA LA FALTA: DESCONTAR DE LA TABLA PRODUCTOS ---
                    var prodBD = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prodBD != null)
                    {
                        prodBD.Stock -= item.Cantidad;
                        if (prodBD.Stock < 0) prodBD.Stock = 0;
                    }
                }

                db.SaveChanges(); // Guarda detalles y nuevos stocks en MySQL
                return nroTicketAtencion;
            }
        }
    }
}