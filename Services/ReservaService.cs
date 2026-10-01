using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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

                    sql += " ORDER BY r.FechaEntregaPactada ASC ";

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

        public decimal ObtenerTotalAbonosEfectivoPorTurno(int cajaTurnoId)
        {
            using (var db = new AppDbContext())
            {
                return db.ReservaPagos
                         .Where(p => p.CajaTurnoID == cajaTurnoId && p.MedioPago == "Efectivo")
                         .Sum(p => (decimal?)p.Monto) ?? 0m;
            }
        }
    }
}