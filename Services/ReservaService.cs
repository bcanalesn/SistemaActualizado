using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ReservaService
    {
        public Reserva RegistrarNuevaReserva(Reserva reserva, ReservaPago? abonoInicial)
        {
            using (var db = new AppDbContext())
            {
                using (var tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Correlativo tipo RES-AAAAMMDD-XXX
                        string prefijoFecha = DateTime.Now.ToString("yyyyMMdd");
                        int countHoy = db.Reservas.Count(r => r.CodigoReserva.StartsWith($"RES-{prefijoFecha}"));
                        reserva.CodigoReserva = $"RES-{prefijoFecha}-{(countHoy + 1):D3}";
                        reserva.FechaRegistro = DateTime.Now;

                        decimal montoAbono = abonoInicial?.Monto ?? 0m;
                        reserva.TotalAbonado = montoAbono;
                        reserva.SaldoPendiente = Math.Max(0m, reserva.TotalPedido - montoAbono);
                        reserva.Estado = "Reservado";
                        reserva.StockDescontado = false;

                        // 2. Guardar encabezado
                        db.Reservas.Add(reserva);
                        db.SaveChanges(); // Genera IdReserva autoincremental

                        // 3. Guardar detalles
                        if (reserva.Detalles != null && reserva.Detalles.Count > 0)
                        {
                            foreach (var det in reserva.Detalles)
                            {
                                det.IdReserva = reserva.IdReserva;
                                if (string.IsNullOrWhiteSpace(det.CodigoBarra))
                                {
                                    det.CodigoBarra = det.ProductoID.ToString();
                                }
                                db.ReservaDetalles.Add(det);
                            }
                            db.SaveChanges();
                        }

                        // 4. Guardar abono inicial si existe
                        if (abonoInicial != null && montoAbono > 0)
                        {
                            abonoInicial.IdReserva = reserva.IdReserva;
                            abonoInicial.FechaPago = DateTime.Now;
                            abonoInicial.EsAbonoInicial = true;
                            abonoInicial.EsLiquidacionFinal = false;
                            db.ReservaPagos.Add(abonoInicial);
                            db.SaveChanges();

                            reserva.Pagos = new List<ReservaPago> { abonoInicial };
                        }

                        tr.Commit();
                        return reserva;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        throw new Exception(msg);
                    }
                }
            }
        }

        public List<Reserva> ObtenerReservas(string filtroEstado = "Todos", string textoBusqueda = "")
        {
            using (var db = new AppDbContext())
            {
                using (var conn = db.Database.GetDbConnection())
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    string sql = @"
                        SELECT r.IdReserva, r.CodigoReserva, r.IdCliente, r.FechaRegistro, r.FechaEntregaPactada, 
                               r.TotalPedido, r.TotalAbonado, r.SaldoPendiente, r.Estado, r.Observaciones, 
                               r.UsuarioRegistro, r.UsuarioEntrega, r.FechaEntregaReal, r.IdDTEGenerado, r.StockDescontado,
                               COALESCE(c.RazonSocial, 'Sin Nombre') AS NombreCliente,
                               COALESCE(c.Telefono, '') AS TelefonoCliente,
                               COALESCE(c.Rut, '') AS RutCliente
                        FROM reserva r
                        LEFT JOIN clientes c ON r.IdCliente = c.IdCliente
                        WHERE 1=1 ";

                    if (filtroEstado != "Todos")
                    {
                        sql += " AND r.Estado = @estado ";
                    }

                    if (!string.IsNullOrWhiteSpace(textoBusqueda))
                    {
                        sql += " AND (r.CodigoReserva LIKE @q OR c.RazonSocial LIKE @q OR c.Rut LIKE @q) ";
                    }

                    sql += " ORDER BY r.FechaRegistro DESC, r.IdReserva DESC ";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;

                        if (filtroEstado != "Todos")
                        {
                            var pEst = cmd.CreateParameter();
                            pEst.ParameterName = "@estado";
                            pEst.Value = filtroEstado;
                            cmd.Parameters.Add(pEst);
                        }

                        if (!string.IsNullOrWhiteSpace(textoBusqueda))
                        {
                            var pQ = cmd.CreateParameter();
                            pQ.ParameterName = "@q";
                            pQ.Value = $"%{textoBusqueda.Trim()}%";
                            cmd.Parameters.Add(pQ);
                        }

                        var lista = new List<Reserva>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new Reserva
                                {
                                    IdReserva = Convert.ToInt32(reader["IdReserva"]),
                                    CodigoReserva = reader["CodigoReserva"]?.ToString() ?? "",
                                    IdCliente = reader["IdCliente"] != DBNull.Value ? Convert.ToInt32(reader["IdCliente"]) : null,
                                    FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                                    FechaEntregaPactada = Convert.ToDateTime(reader["FechaEntregaPactada"]),
                                    TotalPedido = Convert.ToDecimal(reader["TotalPedido"]),
                                    TotalAbonado = Convert.ToDecimal(reader["TotalAbonado"]),
                                    SaldoPendiente = Convert.ToDecimal(reader["SaldoPendiente"]),
                                    Estado = reader["Estado"]?.ToString() ?? "Reservado",
                                    Observaciones = reader["Observaciones"] != DBNull.Value ? reader["Observaciones"].ToString() : null,
                                    UsuarioRegistro = reader["UsuarioRegistro"]?.ToString() ?? "",
                                    UsuarioEntrega = reader["UsuarioEntrega"] != DBNull.Value ? reader["UsuarioEntrega"].ToString() : null,
                                    FechaEntregaReal = reader["FechaEntregaReal"] != DBNull.Value ? Convert.ToDateTime(reader["FechaEntregaReal"]) : null,
                                    IdDTEGenerado = reader["IdDTEGenerado"] != DBNull.Value ? Convert.ToInt32(reader["IdDTEGenerado"]) : (int?)null,
                                    StockDescontado = Convert.ToBoolean(reader["StockDescontado"]),
                                    NombreCliente = reader["NombreCliente"]?.ToString() ?? "",
                                    TelefonoCliente = reader["TelefonoCliente"]?.ToString() ?? "",
                                    RutCliente = reader["RutCliente"]?.ToString() ?? ""
                                });
                            }
                        }

                        return lista;
                    }
                }
            }
        }

        public List<ReservaDetalle> ObtenerDetallesReserva(int idReserva)
        {
            using (var db = new AppDbContext())
            {
                return db.ReservaDetalles.Where(d => d.IdReserva == idReserva).ToList();
            }
        }

        public List<ReservaPago> ObtenerPagosReserva(int idReserva)
        {
            using (var db = new AppDbContext())
            {
                return db.ReservaPagos
                         .Where(p => p.IdReserva == idReserva)
                         .OrderByDescending(p => p.FechaPago)
                         .ToList();
            }
        }

        public void MarcarComoListoParaRetiro(int idReserva)
        {
            using (var db = new AppDbContext())
            {
                var reserva = db.Reservas.FirstOrDefault(r => r.IdReserva == idReserva);
                if (reserva != null && reserva.Estado == "Reservado")
                {
                    reserva.Estado = "Listo para Retiro";
                    db.SaveChanges();
                }
            }
        }

        public void RegistrarAbono(int idReserva, decimal monto, string medioPago, int cajaTurnoId, string usuario)
        {
            using (var db = new AppDbContext())
            {
                using (var tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        var reserva = db.Reservas.FirstOrDefault(r => r.IdReserva == idReserva);
                        if (reserva == null) throw new Exception("Reserva no encontrada");

                        var pago = new ReservaPago
                        {
                            IdReserva = idReserva,
                            Monto = monto,
                            MedioPago = medioPago,
                            FechaPago = DateTime.Now,
                            CajaTurnoID = cajaTurnoId,
                            Usuario = usuario,
                            EsAbonoInicial = false,
                            EsLiquidacionFinal = false
                        };

                        db.ReservaPagos.Add(pago);

                        reserva.TotalAbonado += monto;
                        reserva.SaldoPendiente = Math.Max(0m, reserva.TotalPedido - reserva.TotalAbonado);

                        db.SaveChanges();
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        throw new Exception(msg);
                    }
                }
            }
        }

        public void EntregarYDescontarStock(int idReserva, decimal montoCobrado, string medioPago, int cajaTurnoId, string usuario, int folioBoleta)
        {
            using (var db = new AppDbContext())
            {
                using (var tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        var reserva = db.Reservas.FirstOrDefault(r => r.IdReserva == idReserva);
                        if (reserva == null) throw new Exception("Reserva no encontrada");

                        // 1. Descontar Stock definitivo en el Escenario A mediante Entity Framework
                        var detalles = db.ReservaDetalles.Where(d => d.IdReserva == idReserva).ToList();
                        foreach (var det in detalles)
                        {
                            var prod = db.Productos.FirstOrDefault(p => p.ProductoID == det.ProductoID);
                            if (prod != null)
                            {
                                prod.Stock -= det.Cantidad;
                                if (prod.Stock < 0) prod.Stock = 0;
                            }
                        }

                        // 2. Registrar el cobro del saldo restante si hubo
                        if (montoCobrado > 0)
                        {
                            var pagoFinal = new ReservaPago
                            {
                                IdReserva = idReserva,
                                Monto = montoCobrado,
                                MedioPago = medioPago,
                                FechaPago = DateTime.Now,
                                CajaTurnoID = cajaTurnoId,
                                Usuario = usuario,
                                EsAbonoInicial = false,
                                EsLiquidacionFinal = true
                            };

                            db.ReservaPagos.Add(pagoFinal);
                            reserva.TotalAbonado += montoCobrado;
                            reserva.SaldoPendiente = Math.Max(0m, reserva.TotalPedido - reserva.TotalAbonado);
                        }

                        // 3. Finalizar reserva y asociar documento tributario
                        reserva.Estado = "Entregado";
                        reserva.FechaEntregaReal = DateTime.Now;
                        reserva.UsuarioEntrega = usuario;
                        reserva.IdDTEGenerado = folioBoleta;
                        reserva.StockDescontado = true;

                        db.SaveChanges();
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        throw new Exception(msg);
                    }
                }
            }
        }

        public void AnularReserva(int idReserva)
        {
            using (var db = new AppDbContext())
            {
                var reserva = db.Reservas.FirstOrDefault(r => r.IdReserva == idReserva);
                if (reserva != null)
                {
                    reserva.Estado = "Anulado";
                    db.SaveChanges();
                }
            }
        }
        public (Reserva Reserva, int NroTicketAbono) RegistrarReservaConTicketAbono(Reserva reserva, decimal montoAbono, string usuarioVendedor, string usuarioSesion = "")
        {
            using var db = new AppDbContext();
            using var tr = db.Database.BeginTransaction();
            try
            {
                // 1. Asignar código correlativo de reserva
                string prefijoFecha = DateTime.Now.ToString("yyyyMMdd");
                int countHoy = db.Reservas.Count(r => r.CodigoReserva.StartsWith($"RES-{prefijoFecha}"));
                reserva.CodigoReserva = $"RES-{prefijoFecha}-{(countHoy + 1):D3}";
                reserva.FechaRegistro = DateTime.Now;
                reserva.TotalAbonado = 0m;
                reserva.SaldoPendiente = reserva.TotalPedido;
                reserva.Estado = "Reservado";
                reserva.StockDescontado = false;

                db.Reservas.Add(reserva);
                db.SaveChanges();

                // 2. Guardar detalles del pedido
                if (reserva.Detalles != null)
                {
                    foreach (var det in reserva.Detalles)
                    {
                        det.IdReserva = reserva.IdReserva;
                        if (string.IsNullOrWhiteSpace(det.CodigoBarra))
                        {
                            det.CodigoBarra = det.ProductoID.ToString();
                        }
                        db.ReservaDetalles.Add(det);
                    }
                    db.SaveChanges();
                }

                int nroTicketAbono = 0;

                // 3. Si hay abono inicial, generar Ticket correlativo en TVE2607 para Caja
                if (montoAbono > 0)
                {
                    DateTime ahora = DateTime.Now;
                    int ultimoNroInT = db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                    int siguienteNroInT = ultimoNroInT + 1;
                    nroTicketAbono = ObtenerSiguienteNroTicketCorrelativo(db);

                    var clienteDb = db.Clientes.Find(reserva.IdCliente);

                    decimal netoAbono = Math.Round(montoAbono / 1.19m, 0);
                    decimal ivaAbono = montoAbono - netoAbono;

                    // Diferenciamos quién opera frente al PC de quién comisiona la venta[cite: 20]
                    string usuarioOperador = !string.IsNullOrWhiteSpace(usuarioSesion) ? usuarioSesion : usuarioVendedor;

                    // Razón Social oficial limpia[cite: 20]
                    string razonLimpia = clienteDb != null 
                        ? clienteDb.RazonSocial 
                        : (!string.IsNullOrWhiteSpace(reserva.NombreCliente) ? reserva.NombreCliente : "Consumidor Final");

                    var ticketAbono = new TVE2607
                    {
                        idLocal = 1,
                        nmbLocal = "Local Principal",
                        iddocDTE = 0,
                        Documento = $"Abono Reserva {reserva.CodigoReserva}",
                        NroTicket = nroTicketAbono,
                        FechaTicket = ahora,
                        nroDTE = nroTicketAbono,
                        nroInT = siguienteNroInT,
                        FecDoc = ahora,
                        SubTotal = montoAbono,
                        Descuento = 0,
                        Neto = netoAbono,
                        IvA = ivaAbono,
                        Total = montoAbono,
                        UserDTE = usuarioOperador,
                        Vendedor = usuarioVendedor,
                        Idcliente = clienteDb?.IdCliente ?? 0,
                        RuT = clienteDb != null ? clienteDb.Rut : "",
                        RazonSocial = razonLimpia, // 👈 Limpio, sin corchetes
                        Giro = clienteDb?.Giro ?? "",
                        Direccion = clienteDb?.Direccion ?? "",
                        nComuna = clienteDb?.Comuna ?? "",
                        nCiudad = clienteDb?.Ciudad ?? "",
                        Fono1 = clienteDb?.Telefono ?? "",
                        email = clienteDb?.Email ?? "",
                        status = "Pendiente",
                        idREF = reserva.IdReserva
                    };

                    db.TVE2607.Add(ticketAbono);
                    db.SaveChanges();

                    var detalleAbono = new TVD2607
                    {
                        idTve = ticketAbono.idTve,
                        idLocal = 1,
                        iddocDTE = 0,
                        Documento = ticketAbono.Documento,
                        NroDTE = nroTicketAbono,
                        NroInT = siguienteNroInT,
                        FecMoV = ahora,
                        HoraMoV = ahora.ToString("HH:mm:ss"),
                        IdProducto = 0,
                        NmbProducto = $"Anticipo / Abono Pedido {reserva.CodigoReserva}",
                        Cantidad = 1,
                        Precio = montoAbono,
                        SubTotal = montoAbono,
                        nmbVendedor = usuarioVendedor,
                        Unidad = "UN"
                    };

                    db.TVD2607.Add(detalleAbono);
                    db.SaveChanges();
                }

                tr.Commit();
                return (reserva, nroTicketAbono);
            }
            catch (Exception ex)
            {
                tr.Rollback();
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception(msg);
            }
        }
        public int GenerarTicketLiquidacionCaja(int idReserva, string usuarioOperador)
        {
            using var db = new AppDbContext();
            using var tr = db.Database.BeginTransaction();
            try
            {
                var reserva = db.Reservas.FirstOrDefault(r => r.IdReserva == idReserva);
                if (reserva == null) throw new Exception("Reserva no encontrada.");

                if (reserva.Estado == "Entregado" || reserva.Estado == "Anulado")
                {
                    throw new Exception($"El pedido ya se encuentra en estado '{reserva.Estado}'.");
                }

                // 🛑 CANDADO: Evitar duplicar tickets si ya se envió a caja y sigue pendiente
                var ticketPendienteExistente = db.TVE2607.FirstOrDefault(t => t.idREF == idReserva && t.status == "Pendiente");
                if (ticketPendienteExistente != null)
                {
                    tr.Rollback();
                    return ticketPendienteExistente.NroTicket > 0 ? ticketPendienteExistente.NroTicket : ticketPendienteExistente.nroDTE;
                }

                decimal saldoCobrar = reserva.SaldoPendiente;
                DateTime ahora = DateTime.Now;
                int ultimoNroInT = db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;
                int nroTicket = ObtenerSiguienteNroTicketCorrelativo(db);


                var clienteDb = db.Clientes.Find(reserva.IdCliente);
                decimal neto = Math.Round(saldoCobrar / 1.19m, 0);
                decimal iva = saldoCobrar - neto;

                string vendedorFinal = !string.IsNullOrWhiteSpace(reserva.UsuarioRegistro) ? reserva.UsuarioRegistro : usuarioOperador;

                string razonLimpia = clienteDb != null 
                    ? clienteDb.RazonSocial 
                    : (!string.IsNullOrWhiteSpace(reserva.NombreCliente) ? reserva.NombreCliente : "Consumidor Final");

                var ticketSaldo = new TVE2607
                {
                    idLocal = 1,
                    nmbLocal = "Local Principal",
                    iddocDTE = 0,
                    Documento = $"Saldo Reserva {reserva.CodigoReserva}",
                    NroTicket = nroTicket,
                    FechaTicket = ahora,
                    nroDTE = nroTicket,
                    nroInT = siguienteNroInT,
                    FecDoc = ahora,
                    SubTotal = saldoCobrar,
                    Descuento = 0,
                    Neto = neto,
                    IvA = iva,
                    Total = saldoCobrar,
                    UserDTE = usuarioOperador, // Quien liquidó la entrega
                    Vendedor = vendedorFinal,  // Vendedor original comisionista
                    Idcliente = clienteDb?.IdCliente ?? 0,
                    RuT = clienteDb?.Rut ?? "",
                    RazonSocial = razonLimpia, // 👈 Limpio, sin corchetes
                    Giro = clienteDb?.Giro ?? "",
                    Direccion = clienteDb?.Direccion ?? "",
                    nComuna = clienteDb?.Comuna ?? "",
                    nCiudad = clienteDb?.Ciudad ?? "",
                    Fono1 = clienteDb?.Telefono ?? "",
                    email = clienteDb?.Email ?? "",
                    status = "Pendiente",
                    idREF = reserva.IdReserva
                };

                db.TVE2607.Add(ticketSaldo);
                db.SaveChanges();

                var detalles = db.ReservaDetalles.Where(d => d.IdReserva == idReserva).ToList();
                foreach (var item in detalles)
                {
                    db.TVD2607.Add(new TVD2607
                    {
                        idTve = ticketSaldo.idTve,
                        idLocal = 1,
                        iddocDTE = 0,
                        Documento = ticketSaldo.Documento,
                        NroDTE = nroTicket,
                        NroInT = siguienteNroInT,
                        FecMoV = ahora,
                        HoraMoV = ahora.ToString("HH:mm:ss"),
                        IdProducto = item.ProductoID,
                        NmbProducto = item.NombreProducto,
                        Cantidad = item.Cantidad,
                        Precio = item.PrecioUnitario,
                        SubTotal = item.Subtotal,
                        nmbVendedor = vendedorFinal,
                        Unidad = item.EsPesable ? "gr" : "UN"
                    });

                    // Descontar inventario físico Y disponible
                    var prod = db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prod != null)
                    {
                        prod.Stock -= item.Cantidad;
                        if (prod.Stock < 0) prod.Stock = 0;

                        prod.StockDisponible -= item.Cantidad;
                        if (prod.StockDisponible < 0) prod.StockDisponible = 0;
                    }
                }

                reserva.StockDescontado = true;
                db.SaveChanges();

                tr.Commit();
                return nroTicket;
            }
            catch (Exception ex)
            {
                tr.Rollback();
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception(msg);
            }
        }

        public decimal ObtenerTotalAbonosEfectivoPorTurno(int cajaTurnoId)
        {
            using (var db = new AppDbContext())
            {
                return db.ReservaPagos
                         .Where(p => p.CajaTurnoID == cajaTurnoId && p.MedioPago == "Efectivo")
                         .Sum(p => (decimal?)p.Monto) ?? 0m;
            }
        }

        private int ObtenerSiguienteNroTicketCorrelativo(AppDbContext db)
        {
            const int LIMITE_TICKET = 999999;

            int ultimoTicket = db.TVE2607
                .OrderByDescending(v => v.idTve)
                .Select(v => v.NroTicket)
                .FirstOrDefault();

            if (ultimoTicket <= 0 || ultimoTicket >= LIMITE_TICKET)
            {
                return 1;
            }

            return ultimoTicket + 1;
        }

        public int GenerarTicketAbonoPosteriorACaja(int idReserva, decimal montoAbono, string usuarioVendedor, string usuarioSesion = "")
        {
            using var db = new AppDbContext();
            using var tr = db.Database.BeginTransaction();
            try
            {
                var reserva = db.Reservas.FirstOrDefault(r => r.IdReserva == idReserva);
                if (reserva == null) throw new Exception("Reserva no encontrada.");

                if (reserva.Estado == "Entregado" || reserva.Estado == "Anulado")
                {
                    throw new Exception($"La reserva ya se encuentra en estado '{reserva.Estado}'. No admite más abonos.");
                }

                // 🛑 CANDADO 1: Verificar si ya existe un ticket de abono o liquidación pendiente en Caja
                var ticketPendiente = db.TVE2607.FirstOrDefault(t => t.idREF == idReserva && t.status == "Pendiente");
                if (ticketPendiente != null)
                {
                    throw new Exception($"La reserva ya tiene el Ticket N° {ticketPendiente.NroTicket} pendiente de cobro en Caja por {MonedaHelper.Formatear(ticketPendiente.Total, conSigno: true)}.\nDebe cobrarlo o anularlo en Caja antes de emitir un nuevo abono.");
                }

                // 🛑 CANDADO 2: El abono no puede superar el saldo pendiente actual
                if (montoAbono > reserva.SaldoPendiente)
                {
                    throw new Exception($"El monto a abonar ({MonedaHelper.Formatear(montoAbono, conSigno: true)}) supera el saldo pendiente ({MonedaHelper.Formatear(reserva.SaldoPendiente, conSigno: true)}).");
                }

                DateTime ahora = DateTime.Now;
                int ultimoNroInT = db.TVE2607.Max(v => (int?)v.nroInT) ?? 0;
                int siguienteNroInT = ultimoNroInT + 1;
                int nroTicketAbono = ObtenerSiguienteNroTicketCorrelativo(db);

                var clienteDb = db.Clientes.Find(reserva.IdCliente);
                decimal netoAbono = Math.Round(montoAbono / 1.19m, 0);
                decimal ivaAbono = montoAbono - netoAbono;

                // Conservar el vendedor original que tomó la reserva[cite: 20]
                string vendedorFinal = !string.IsNullOrWhiteSpace(usuarioVendedor) ? usuarioVendedor : reserva.UsuarioRegistro;
                string operadorActivo = !string.IsNullOrWhiteSpace(usuarioSesion) ? usuarioSesion : vendedorFinal;

                string razonLimpia = clienteDb != null 
                    ? clienteDb.RazonSocial 
                    : (!string.IsNullOrWhiteSpace(reserva.NombreCliente) ? reserva.NombreCliente : "Consumidor Final");

                var ticketAbono = new TVE2607
                {
                    idLocal = 1,
                    nmbLocal = "Local Principal",
                    iddocDTE = 0,
                    Documento = $"Abono Reserva {reserva.CodigoReserva}",
                    NroTicket = nroTicketAbono,
                    FechaTicket = ahora,
                    nroDTE = nroTicketAbono,
                    nroInT = siguienteNroInT,
                    FecDoc = ahora,
                    SubTotal = montoAbono,
                    Descuento = 0,
                    Neto = netoAbono,
                    IvA = ivaAbono,
                    Total = montoAbono,
                    UserDTE = operadorActivo,
                    Vendedor = vendedorFinal, 
                    Idcliente = clienteDb?.IdCliente ?? 0,
                    RuT = clienteDb != null ? clienteDb.Rut : "",
                    RazonSocial = razonLimpia, // 👈 Limpio, sin corchetes
                    Giro = clienteDb?.Giro ?? "",
                    Direccion = clienteDb?.Direccion ?? "",
                    nComuna = clienteDb?.Comuna ?? "",
                    nCiudad = clienteDb?.Ciudad ?? "",
                    Fono1 = clienteDb?.Telefono ?? "",
                    email = clienteDb?.Email ?? "",
                    status = "Pendiente",
                    idREF = reserva.IdReserva
                };

                db.TVE2607.Add(ticketAbono);
                db.SaveChanges();

                var detalleAbono = new TVD2607
                {
                    idTve = ticketAbono.idTve,
                    idLocal = 1,
                    iddocDTE = 0,
                    Documento = ticketAbono.Documento,
                    NroDTE = nroTicketAbono,
                    NroInT = siguienteNroInT,
                    FecMoV = ahora,
                    HoraMoV = ahora.ToString("HH:mm:ss"),
                    IdProducto = 0,
                    NmbProducto = $"Abono Parcial Pedido {reserva.CodigoReserva}",
                    Cantidad = 1,
                    Precio = montoAbono,
                    SubTotal = montoAbono,
                    nmbVendedor = vendedorFinal,
                    Unidad = "UN"
                };

                db.TVD2607.Add(detalleAbono);
                db.SaveChanges();

                tr.Commit();
                return nroTicketAbono;
            }
            catch (Exception ex)
            {
                tr.Rollback();
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception(msg);
            }
        }
    }
}