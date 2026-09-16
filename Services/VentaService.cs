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
        // 1. GENERACIÓN DE TICKET DIRECTO A CAJA (Descuenta stock inmediatamente)
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

        // 2. VENTAS EN ESPERA: GUARDAR EN PAUSA (PRESERVA RAZÓN SOCIAL Y NO TOCA STOCK)
        public int PonerVentaEnEspera(List<DetalleCarrito> carrito, string vendedor, string clienteNombre, string clienteRut, string identificador, int idTveExistente = 0)
        {
            using var db = new AppDbContext();
            TVE2607? venta = null;

            if (idTveExistente > 0)
            {
                venta = db.TVE2607.FirstOrDefault(v => v.idTve == idTveExistente);
            }

            string rutLimpio = RutHelper.Limpiar(clienteRut ?? "");
            var clienteDb = db.Clientes.FirstOrDefault(c => !string.IsNullOrEmpty(rutLimpio) && (c.Rut == rutLimpio || c.Rut == clienteRut));

            string nombreBaseLimpio = System.Text.RegularExpressions.Regex.Replace(clienteNombre ?? "Consumidor Final", @"\s*\([^)]*\)", "").Trim();
            if (string.IsNullOrWhiteSpace(nombreBaseLimpio)) nombreBaseLimpio = "Consumidor Final";

            string razonSocialFinal;
            if (!string.IsNullOrEmpty(rutLimpio) && clienteDb != null)
            {
                razonSocialFinal = clienteDb.RazonSocial;
            }
            else
            {
                razonSocialFinal = !string.IsNullOrWhiteSpace(identificador) 
                    ? $"{nombreBaseLimpio} ({identificador.Trim()})" 
                    : nombreBaseLimpio;
            }

            decimal subtotal = carrito.Sum(c => c.Subtotal);

            if (venta == null)
            {
                int ultimoNroInT = db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;
                int nroTicket = (int)(DateTime.Now.Ticks % 1000000);

                venta = new TVE2607
                {
                    idLocal = 1,
                    nmbLocal = "Local Principal",
                    iddocDTE = 0,
                    Documento = "Venta en Espera",
                    nroDTE = nroTicket,
                    nroInT = siguienteNroInT,
                    FecDoc = DateTime.Now,
                    SubTotal = subtotal,
                    Descuento = 0,
                    Neto = Math.Round(subtotal / 1.19m, 0),
                    IvA = subtotal - Math.Round(subtotal / 1.19m, 0),
                    Total = subtotal,
                    UserDTE = vendedor,
                    Vendedor = vendedor,
                    Idcliente = clienteDb?.IdCliente ?? 0,
                    RuT = clienteDb != null ? RutHelper.Formatear(clienteDb.Rut) : "",
                    RazonSocial = razonSocialFinal,
                    Giro = clienteDb?.Giro ?? "",
                    Direccion = clienteDb?.Direccion ?? "",
                    nComuna = clienteDb?.Comuna ?? "",
                    nCiudad = clienteDb?.Ciudad ?? "",
                    Fono1 = clienteDb?.Telefono ?? "",
                    email = clienteDb?.Email ?? "",
                    status = "EnEspera"
                };
                db.TVE2607.Add(venta);
            }
            else
            {
                venta.SubTotal = subtotal;
                venta.Neto = Math.Round(subtotal / 1.19m, 0);
                venta.IvA = subtotal - venta.Neto;
                venta.Total = subtotal;
                venta.UserDTE = vendedor;
                venta.Vendedor = vendedor;
                venta.RazonSocial = razonSocialFinal;
                venta.FecDoc = DateTime.Now;
                venta.status = "EnEspera";

                var detallesAntiguos = db.TVD2607.Where(d => d.idTve == venta.idTve).ToList();
                db.TVD2607.RemoveRange(detallesAntiguos);
            }

            db.SaveChanges();

            foreach (var item in carrito)
            {
                var detalle = new TVD2607
                {
                    idTve = venta.idTve,
                    idLocal = 1,
                    iddocDTE = 0,
                    Documento = "Venta en Espera",
                    NroDTE = venta.nroDTE,
                    NroInT = venta.nroInT,
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
            }

            db.SaveChanges();
            return venta.idTve;
        }

        // 3. CONSULTAR VENTAS EN ESPERA ACTIVAS DEL DÍA
        public List<TVE2607> ObtenerVentasEnEspera()
        {
            using var db = new AppDbContext();
            DateTime inicioHoy = DateTime.Today;
            DateTime limiteHoras = DateTime.Now.AddHours(-6);
            DateTime fechaFiltro = limiteHoras > inicioHoy ? limiteHoras : inicioHoy;

            return db.TVE2607
                .AsNoTracking()
                .Where(v => (v.status == "EnEspera" || v.status == "EnUso") && v.FecDoc >= fechaFiltro)
                .OrderByDescending(v => v.FecDoc)
                .ToList();
        }

        // 4. RECUPERAR VENTA CON CONTROL ESTRICTO DE PROPIEDAD
        public (bool Exito, string Mensaje, List<DetalleCarrito> Carrito, string Rut, string Cliente, int IdCliente) RecuperarVentaEnEspera(int idTve, string vendedorActual)
        {
            using var db = new AppDbContext();
            var venta = db.TVE2607.FirstOrDefault(v => v.idTve == idTve);
            if (venta == null) 
                return (false, "La venta seleccionada ya no existe.", new List<DetalleCarrito>(), "", "", 0);

            // Verificación de dueño
            if (!string.Equals(venta.Vendedor, vendedorActual, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Esta venta fue iniciada por '{venta.Vendedor}'. Solo dicho vendedor puede reanudarla.", new List<DetalleCarrito>(), "", "", 0);
            }

            if (venta.status == "EnUso" && !string.Equals(venta.UserDTE, vendedorActual, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Esta venta ya está abierta por '{venta.UserDTE}'.", new List<DetalleCarrito>(), "", "", 0);
            }

            venta.status = "EnUso";
            venta.UserDTE = vendedorActual;
            db.SaveChanges();

            var detalles = db.TVD2607.Where(d => d.idTve == idTve).ToList();
            var carrito = new List<DetalleCarrito>();

            foreach (var d in detalles)
            {
                carrito.Add(new DetalleCarrito
                {
                    ProductoID = d.IdProducto,
                    Nombre = d.NmbProducto ?? "Producto",
                    PrecioUnitario = d.Precio,
                    Cantidad = d.Cantidad
                });
            }

            return (true, "OK", carrito, venta.RuT ?? "", venta.RazonSocial ?? "Consumidor Final", venta.Idcliente);
        }

        // 5. ENVIAR A CAJA VENTA PREVIAMENTE EN ESPERA (DESCUENTA STOCK DEFINITIVO)
        public int ActualizarTicketExistenteACaja(int idTve, List<DetalleCarrito> carrito, string vendedor, string clienteNombre, string clienteRut)
        {
            using var db = new AppDbContext();
            var venta = db.TVE2607.FirstOrDefault(v => v.idTve == idTve);
            if (venta == null)
            {
                return GenerarTicketVenta(carrito, vendedor, clienteNombre, clienteRut);
            }

            string rutLimpio = RutHelper.Limpiar(clienteRut ?? "");
            var clienteDb = db.Clientes.FirstOrDefault(c => !string.IsNullOrEmpty(rutLimpio) && (c.Rut == rutLimpio || c.Rut == clienteRut));

            decimal subtotal = carrito.Sum(c => c.Subtotal);
            venta.SubTotal = subtotal;
            venta.Neto = Math.Round(subtotal / 1.19m, 0);
            venta.IvA = subtotal - venta.Neto;
            venta.Total = subtotal;
            venta.UserDTE = vendedor;
            venta.Vendedor = vendedor;
            venta.FecDoc = DateTime.Now;
            venta.Documento = "Ticket de Atención";
            venta.status = "Pendiente";

            if (clienteDb != null)
            {
                venta.Idcliente = clienteDb.IdCliente;
                venta.RuT = RutHelper.Formatear(clienteDb.Rut);
                venta.RazonSocial = clienteDb.RazonSocial;
                venta.Giro = clienteDb.Giro ?? "";
            }

            var anteriores = db.TVD2607.Where(d => d.idTve == idTve).ToList();
            db.TVD2607.RemoveRange(anteriores);

            foreach (var item in carrito)
            {
                var detalle = new TVD2607
                {
                    idTve = venta.idTve,
                    idLocal = 1,
                    iddocDTE = 0,
                    Documento = "Ticket de Atención",
                    NroDTE = venta.nroDTE,
                    NroInT = venta.nroInT,
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
            return venta.nroDTE;
        }

        // 6. DESCARTAR VENTA EN ESPERA
        public void CancelarVentaEnEspera(int idTve)
        {
            using var db = new AppDbContext();
            var venta = db.TVE2607.FirstOrDefault(v => v.idTve == idTve);
            if (venta == null) return;

            venta.status = "Anulado";
            db.SaveChanges();
        }

        // 7. CONSULTAS Y HISTORIAL
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