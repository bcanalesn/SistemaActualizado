using System;
using System.Collections.Generic;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class NotaCreditoService
    {
        private readonly AppDbContext _db;

        public NotaCreditoService(AppDbContext db)
        {
            _db = db;
        }

        public bool EmitirNotaCredito(int idTveOrigen, string motivo, string codigoREF, bool reponerStock)
        {
            return EmitirNotasCredito(new[] { idTveOrigen }, motivo, codigoREF, reponerStock) > 0;
        }

        public int EmitirNotasCredito(IEnumerable<int> idsTveOrigen, string motivo, string codigoREF, bool reponerStock)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var idsValidos = idsTveOrigen.Distinct().ToList();
                if (idsValidos.Count == 0)
                {
                    return 0;
                }

                var ventasOrigen = _db.TVE2607
                    .Where(v => idsValidos.Contains(v.idTve))
                    .ToList();

                int emitidas = 0;

                foreach (var ventaOrigen in ventasOrigen)
                {
                    if (ventaOrigen.iddocDTE == 61 || ventaOrigen.status.Equals("Anulado NC", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var ncHeader = new TVE2607
                    {
                        idLocal = ventaOrigen.idLocal,
                        nmbLocal = ventaOrigen.nmbLocal,
                        iddocDTE = 61,
                        Documento = "Nota de Crédito Electrónica",
                        nroDTE = (int)(DateTime.Now.Ticks % 100000),
                        FecDoc = DateTime.Now,
                        HoraDoc = DateTime.Now.ToString("HH:mm:ss"),
                        SubTotal = ventaOrigen.SubTotal,
                        Descuento = ventaOrigen.Descuento,
                        Neto = ventaOrigen.Neto,
                        IvA = ventaOrigen.IvA,
                        Total = ventaOrigen.Total,
                        RuT = ventaOrigen.RuT,
                        RazonSocial = ventaOrigen.RazonSocial,
                        Giro = ventaOrigen.Giro,
                        idREF = ventaOrigen.idTve,
                        nroREF = ventaOrigen.nroDTE,
                        codigoREF = codigoREF,
                        UserDTE = ventaOrigen.UserDTE,
                        Vendedor = ventaOrigen.Vendedor,
                        status = "Emitido"
                    };

                    _db.TVE2607.Add(ncHeader);
                    _db.SaveChanges();

                    var detallesOrigen = _db.TVD2607.Where(d => d.idTve == ventaOrigen.idTve).ToList();

                    foreach (var item in detallesOrigen)
                    {
                        var ncDetalle = new TVD2607
                        {
                            idTve = ncHeader.idTve,
                            idLocal = item.idLocal,
                            iddocDTE = 61,
                            Documento = "Nota de Crédito Electrónica",
                            NroDTE = ncHeader.nroDTE,
                            FecMoV = DateTime.Now,
                            HoraMoV = DateTime.Now.ToString("HH:mm:ss"),
                            IdProducto = item.IdProducto,
                            NmbProducto = item.NmbProducto,
                            Cantidad = item.Cantidad,
                            Precio = item.Precio,
                            PneTo = item.PneTo,
                            SubTotal = item.SubTotal,
                            SubNeto = item.SubNeto,
                            nmbVendedor = item.nmbVendedor
                        };
                        _db.TVD2607.Add(ncDetalle);

                        if (reponerStock)
                        {
                            var prod = _db.Productos.FirstOrDefault(p => p.ProductoID == item.IdProducto);
                            if (prod != null)
                            {
                                prod.Stock += item.Cantidad;
                            }
                        }
                    }

                    ventaOrigen.status = "Anulado NC";
                    emitidas++;
                }

                _db.SaveChanges();
                transaction.Commit();
                return emitidas;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}