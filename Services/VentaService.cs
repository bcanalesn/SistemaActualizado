using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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

        public List<TVD2607> ObtenerDetallesVenta(int idTve)
        {
            using var db = new AppDbContext();
            return db.TVD2607.AsNoTracking().Where(d => d.idTve == idTve).ToList();
        }

        public List<TicketVendedorDTO> ObtenerMisTickets(string nombreVendedor, DateTime fecha, string? filtroEstado = null, string? busqueda = null)
        {
            using var db = new AppDbContext();
            DateTime inicio = fecha.Date;
            DateTime fin = fecha.Date.AddDays(1).AddTicks(-1);

            var query = db.TVE2607
                .AsNoTracking()
                .Where(v => v.FecDoc >= inicio && v.FecDoc <= fin);

            if (!string.IsNullOrWhiteSpace(nombreVendedor) && nombreVendedor != "Todos")
            {
                string vLower = nombreVendedor.Trim().ToLower();
                query = query.Where(v => (v.Vendedor != null && v.Vendedor.ToLower() == vLower) ||
                                        (v.UserDTE != null && v.UserDTE.ToLower() == vLower));
            }

            if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
            {
                if (filtroEstado == "Enviado a caja") query = query.Where(v => v.status == "Pendiente");
                else if (filtroEstado == "Pagado") query = query.Where(v => v.status == "Emitido");
                else if (filtroEstado == "Anulado") query = query.Where(v => v.status == "Anulado");
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string q = busqueda.Trim().ToLower();
                query = query.Where(v => v.nroDTE.ToString().Contains(q) ||
                                        (v.RazonSocial != null && v.RazonSocial.ToLower().Contains(q)) ||
                                        (v.RuT != null && v.RuT.ToLower().Contains(q)));
            }

            return query
                .OrderByDescending(v => v.FecDoc)
                .Select(v => new TicketVendedorDTO
                {
                    IdTve = v.idTve,
                    NroTicket = v.nroDTE,
                    FechaHora = v.FecDoc,
                    Cliente = string.IsNullOrWhiteSpace(v.RazonSocial) ? "Consumidor Final" : v.RazonSocial,
                    Total = v.Total,
                    EstadoBD = v.status ?? "",
                    EstadoVisual = v.status == "Emitido" ? "🟢 Pagado" :
                                   v.status == "Pendiente" ? "🟡 Enviado a caja" : "🔴 Anulado"
                })
                .ToList();
        }

        public TVE2607? ObtenerVentaPorId(int idTve)
        {
            using var db = new AppDbContext();
            return db.TVE2607.AsNoTracking().FirstOrDefault(v => v.idTve == idTve);
        }
    }

    public class TicketVendedorDTO
    {
        public int IdTve { get; set; }
        public int NroTicket { get; set; }
        public DateTime FechaHora { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string EstadoBD { get; set; } = string.Empty;
        public string EstadoVisual { get; set; } = string.Empty;
    }
}