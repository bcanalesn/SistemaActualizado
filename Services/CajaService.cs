using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Helpers;

namespace SISTEMAACTUALIZADO.Services
{
    public class DesgloseMedioPago
    {
        public string Medio { get; set; } = "Efectivo";
        public decimal Monto { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class MetricasTurno
    {
        public decimal VentasTotales { get; set; }
        public decimal VentasEfectivo { get; set; }
        public decimal VentasTarjetas { get; set; }
        public decimal VentasTransferencia { get; set; }
        public decimal VentasCreditoComercial { get; set; }
        public decimal MontoAnulaciones { get; set; }
        public int CantVentas { get; set; }
        public int CantAnulaciones { get; set; }
        public int CantProductos { get; set; }
        public List<DesgloseMedioPago> ListaDesglose { get; set; } = new List<DesgloseMedioPago>();
    }

    public class CajaService
    {
        // 1. GESTIÓN DE TURNOS PERSISTIDOS EN BD
        public CajaTurno? ObtenerTurnoAbierto(string usuario)
        {
            using var db = new AppDbContext();
            return db.CajaTurnos.FirstOrDefault(t => t.Usuario == usuario && t.Estado == "Abierta");
        }

        public CajaTurno AbrirTurno(string usuario, decimal montoInicial)
        {
            using var db = new AppDbContext();
            var turnoExistente = db.CajaTurnos.FirstOrDefault(t => t.Usuario == usuario && t.Estado == "Abierta");
            if (turnoExistente != null) return turnoExistente;

            var nuevoTurno = new CajaTurno
            {
                Usuario = usuario,
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                Estado = "Abierta"
            };

            db.CajaTurnos.Add(nuevoTurno);
            db.SaveChanges();
            return nuevoTurno;
        }

        public void CerrarTurno(int turnoId, decimal efectivoReal, string? observaciones)
        {
            using var db = new AppDbContext();
            var turno = db.CajaTurnos.Find(turnoId);
            if (turno == null) return;

            var metricas = ObtenerMetricasResumenTurno(turnoId);
            decimal esperado = turno.MontoInicial + metricas.VentasEfectivo;

            turno.FechaCierre = DateTime.Now;
            turno.MontoEfectivoVentas = metricas.VentasEfectivo;
            turno.MontoTarjetaVentas = metricas.VentasTarjetas;
            turno.MontoTransferenciaVentas = metricas.VentasTransferencia;
            turno.MontoCreditoComercial = metricas.VentasCreditoComercial;
            turno.MontoEfectivoReal = efectivoReal;
            turno.Diferencia = efectivoReal - esperado;
            turno.Observaciones = observaciones ?? string.Empty;
            turno.Estado = "Cerrada";

            db.SaveChanges();
        }

        // 2. TICKETS
        public List<TVE2607> ObtenerTicketsPendientes(string filtro = "")
        {
            using var db = new AppDbContext();
            var query = db.TVE2607.Where(v => v.status == "Pendiente");

            if (!string.IsNullOrEmpty(filtro))
            {
                query = query.Where(v => v.nroDTE.ToString().Contains(filtro) || (v.RazonSocial != null && v.RazonSocial.Contains(filtro)));
            }

            return query.OrderByDescending(v => v.FecDoc).ToList();
        }

        public List<TVD2607> ObtenerDetallesTicket(int idTve)
        {
            using var db = new AppDbContext();
            return db.TVD2607.Where(d => d.idTve == idTve).ToList();
        }

        // 3. PROCESAR COBRO: Actualiza FecDoc/HoraDoc al instante de cobro y asigna el Cajero y Turno
        public int ProcesarCobroTicket(TVE2607 ticket, string tipoDoc, string medioPago, decimal vuelto, int turnoId, string cajeroUsuario)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            using var db = new AppDbContext();
            int iddoc = tipoDoc.Contains("Factura") ? 33 : 39;

            var rangoFolio = db.Folios.FirstOrDefault(r => r.TipoDocumento == tipoDoc && r.Activo);
            int folioOficial;
            if (rangoFolio != null && rangoFolio.FolioActual <= rangoFolio.FolioHasta)
            {
                folioOficial = rangoFolio.FolioActual;
                rangoFolio.FolioActual++;
            }
            else
            {
                folioOficial = (int)(DateTime.Now.Ticks % 100000);
            }

            var ticketBd = db.TVE2607.Find(ticket.idTve);
            if (ticketBd == null) throw new InvalidOperationException("El ticket no existe en la base de datos.");

            // 1. Buscar los datos completos del cliente en la BD
            string rutLimpio = RutHelper.Limpiar(ticket.RuT ?? "");
            var clienteDb = db.Clientes.FirstOrDefault(c => 
                (ticket.Idcliente > 0 && c.IdCliente == ticket.Idcliente) ||
                (!string.IsNullOrEmpty(rutLimpio) && (c.Rut == rutLimpio || c.Rut == ticket.RuT)));

            DateTime ahora = DateTime.Now;
            ticketBd.CajaTurnoID = turnoId;
            ticketBd.iddocDTE = iddoc;
            ticketBd.Documento = tipoDoc;
            ticketBd.nroDTE = folioOficial;
            ticketBd.FecDoc = ahora;
            ticketBd.HoraDoc = ahora.ToString("HH:mm:ss");
            ticketBd.UserDTE = cajeroUsuario;
            ticketBd.MedioPago = medioPago;
            ticketBd.Vuelto = vuelto;
            ticketBd.status = "Emitido";

            // 2. Traspaso completo de datos del cliente
            if (clienteDb != null)
            {
                ticketBd.Idcliente = clienteDb.IdCliente;
                ticketBd.RuT = RutHelper.Formatear(clienteDb.Rut);
                ticketBd.RazonSocial = clienteDb.RazonSocial;
                ticketBd.Giro = !string.IsNullOrWhiteSpace(clienteDb.Giro) ? clienteDb.Giro : "PARTICULAR";
                ticketBd.Direccion = clienteDb.Direccion ?? "";
                ticketBd.nComuna = clienteDb.Comuna ?? "SANTIAGO";
                ticketBd.nCiudad = clienteDb.Ciudad ?? "SANTIAGO";
                ticketBd.Fono1 = clienteDb.Telefono ?? "";
                ticketBd.email = clienteDb.Email ?? "";
            }
            else
            {
                if (iddoc == 33)
                {
                    throw new InvalidOperationException("No se puede emitir Factura Electrónica sin un cliente formal registrado. Seleccione o cree un cliente con RUT, Giro y Dirección.");
                }

                // Boleta a consumidor final genérico
                ticketBd.Idcliente = 0;
                ticketBd.RuT = "";
                ticketBd.RazonSocial = "Consumidor Final";
                ticketBd.Giro = "PARTICULAR";
                ticketBd.Direccion = "";
                ticketBd.nComuna = "";
                ticketBd.nCiudad = "";
            }

            // 3. Sincronizar también las líneas de detalle
            var detalles = db.TVD2607.Where(d => d.idTve == ticket.idTve).ToList();
            foreach (var item in detalles)
            {
                item.iddocDTE = iddoc;
                item.Documento = tipoDoc;
                item.NroDTE = folioOficial;
                item.NroInT = ticketBd.nroInT;
            }

            db.SaveChanges();

            // 4. Copiar los datos reales al objeto en memoria para la impresión inmediata
            ticket.CajaTurnoID = ticketBd.CajaTurnoID;
            ticket.iddocDTE = ticketBd.iddocDTE;
            ticket.Documento = ticketBd.Documento;
            ticket.nroDTE = ticketBd.nroDTE;
            ticket.FecDoc = ticketBd.FecDoc;
            ticket.HoraDoc = ticketBd.HoraDoc;
            ticket.UserDTE = ticketBd.UserDTE;
            ticket.MedioPago = ticketBd.MedioPago;
            ticket.Vuelto = ticketBd.Vuelto;
            ticket.status = ticketBd.status;
            ticket.Idcliente = ticketBd.Idcliente;
            ticket.RuT = ticketBd.RuT;
            ticket.RazonSocial = ticketBd.RazonSocial;
            ticket.Giro = ticketBd.Giro;
            ticket.Direccion = ticketBd.Direccion;
            ticket.nComuna = ticketBd.nComuna;
            ticket.nCiudad = ticketBd.nCiudad;
            ticket.Fono1 = ticketBd.Fono1;
            ticket.email = ticketBd.email;

            return folioOficial;
        }

        public void AnularTicket(int idTve, int? turnoId = null)
        {
            using var db = new AppDbContext();
            var ticket = db.TVE2607.FirstOrDefault(t => t.idTve == idTve);
            if (ticket == null) return;

            ticket.status = "Anulado";
            if (turnoId.HasValue) ticket.CajaTurnoID = turnoId.Value;

            var detalles = db.TVD2607.Where(d => d.idTve == idTve).ToList();
            foreach (var item in detalles)
            {
                var prodBD = db.Productos.FirstOrDefault(p => p.ProductoID == item.IdProducto);
                if (prodBD != null)
                {
                    prodBD.Stock += item.Cantidad;
                }
            }

            db.SaveChanges();
        }

        // 4. CÁLCULO DE EFECTIVO AISLADO POR TURNO
        public decimal CalcularVentasEfectivo(int turnoId)
        {
            try
            {
                using var db = new AppDbContext();
                var ventasEmitidas = db.TVE2607
                    .Where(v => v.CajaTurnoID == turnoId && v.status == "Emitido")
                    .ToList();

                decimal totalEfectivo = 0;
                foreach (var v in ventasEmitidas)
                {
                    string medio = v.MedioPago ?? "";
                    if (medio.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        totalEfectivo += (v.iddocDTE == 61 ? -v.Total : v.Total);
                    }
                    else if (medio.StartsWith("Múltiple", StringComparison.OrdinalIgnoreCase))
                    {
                        // Tolerancia a espacios antes y después del signo $
                        var matchEfec = Regex.Match(medio, @"Efec:\s*\$?\s*([\d\.,]+)");
                        if (matchEfec.Success)
                        {
                            decimal efecMonto = MonedaHelper.Limpiar(matchEfec.Groups[1].Value);
                            totalEfectivo += (efecMonto - v.Vuelto);
                        }
                    }
                }

                return totalEfectivo;
            }
            catch
            {
                return 0;
            }
        }

        // 5. MÉTRICAS COMPLETAS DEL TURNO (Por CajaTurnoID para no mezclar cajeros)
        public MetricasTurno ObtenerMetricasResumenTurno(int turnoId)
        {
            var metricas = new MetricasTurno();
            try
            {
                using var db = new AppDbContext();
                var ventasEmitidas = db.TVE2607
                    .Where(v => v.CajaTurnoID == turnoId && v.status == "Emitido")
                    .ToList();

                var anulaciones = db.TVE2607
                    .Where(v => v.CajaTurnoID == turnoId && v.status == "Anulado")
                    .ToList();

                metricas.VentasTotales = ventasEmitidas.Sum(v => v.Total);
                metricas.CantVentas = ventasEmitidas.Count;
                metricas.MontoAnulaciones = anulaciones.Sum(v => v.Total);
                metricas.CantAnulaciones = anulaciones.Count;

                var idsEmitidos = ventasEmitidas.Select(v => v.idTve).ToList();
                metricas.CantProductos = db.TVD2607
                    .Where(d => idsEmitidos.Contains(d.idTve))
                    .Sum(d => (int?)d.Cantidad) ?? 0;

                decimal mEfectivo = 0;
                decimal mDebito = 0;
                decimal mTarjetaCredito = 0;
                decimal mCreditoComercial = 0;
                decimal mTransferencia = 0;

                foreach (var v in ventasEmitidas)
                {
                    string medio = (v.MedioPago ?? "").Trim();

                    if (medio.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        mEfectivo += v.Total;
                    }
                    else if (medio.Equals("Crédito Comercial", StringComparison.OrdinalIgnoreCase) || medio.Contains("PLAZO"))
                    {
                        mCreditoComercial += v.Total;
                    }
                    else if (medio.Equals("Débito", StringComparison.OrdinalIgnoreCase))
                    {
                        mDebito += v.Total;
                    }
                    else if (medio.Equals("Tarjeta Crédito", StringComparison.OrdinalIgnoreCase) || medio.Equals("Crédito", StringComparison.OrdinalIgnoreCase))
                    {
                        mTarjetaCredito += v.Total;
                    }
                    else if (medio.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
                    {
                        mTransferencia += v.Total;
                    }
                    else if (medio.StartsWith("Múltiple", StringComparison.OrdinalIgnoreCase))
                    {
                        // Regex flexible con \s* antes y después de \$
                        var matchEfec = Regex.Match(medio, @"Efec:\s*\$?\s*([\d\.,]+)");
                        var matchTarj = Regex.Match(medio, @"Tarj:\s*\$?\s*([\d\.,]+)");
                        var matchTransf = Regex.Match(medio, @"Transf:\s*\$?\s*([\d\.,]+)");

                        decimal ef = matchEfec.Success ? MonedaHelper.Limpiar(matchEfec.Groups[1].Value) : 0;
                        decimal tar = matchTarj.Success ? MonedaHelper.Limpiar(matchTarj.Groups[1].Value) : 0;
                        decimal tr = matchTransf.Success ? MonedaHelper.Limpiar(matchTransf.Groups[1].Value) : 0;

                        mEfectivo += (ef - v.Vuelto);
                        mDebito += tar;
                        mTransferencia += tr;
                    }
                    else
                    {
                        mEfectivo += v.Total;
                    }
                }

                metricas.VentasEfectivo = mEfectivo;
                metricas.VentasTarjetas = mDebito + mTarjetaCredito;
                metricas.VentasTransferencia = mTransferencia;
                metricas.VentasCreditoComercial = mCreditoComercial;

                decimal total = metricas.VentasTotales > 0 ? metricas.VentasTotales : 1;

                if (mEfectivo > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Efectivo", Monto = mEfectivo, Porcentaje = Math.Round((mEfectivo / total) * 100, 1) });
                if (mCreditoComercial > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Crédito Comercial", Monto = mCreditoComercial, Porcentaje = Math.Round((mCreditoComercial / total) * 100, 1) });
                if (mDebito > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Débito", Monto = mDebito, Porcentaje = Math.Round((mDebito / total) * 100, 1) });
                if (mTarjetaCredito > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Tarjeta Crédito", Monto = mTarjetaCredito, Porcentaje = Math.Round((mTarjetaCredito / total) * 100, 1) });
                if (mTransferencia > 0)
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Transferencia", Monto = mTransferencia, Porcentaje = Math.Round((mTransferencia / total) * 100, 1) });

                if (metricas.ListaDesglose.Count == 0)
                {
                    metricas.ListaDesglose.Add(new DesgloseMedioPago { Medio = "Efectivo", Monto = 0, Porcentaje = 100 });
                }
            }
            catch { }

            return metricas;
        }
    }
}