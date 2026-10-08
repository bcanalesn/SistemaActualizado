using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Models.Enums;

namespace SISTEMAACTUALIZADO.Services
{
    public class CalculoTarifaDeliveryDTO
    {
        public bool EsValido { get; set; }
        public string MensajeError { get; set; } = string.Empty;
        public decimal CostoFinal { get; set; }
        public bool AplicaEnvioGratis { get; set; }
        public decimal MontoFaltanteMinimo { get; set; }
        public decimal MontoFaltanteEnvioGratis { get; set; }
        public int TiempoEstimadoMinutos { get; set; }
        public string NombreZona { get; set; } = string.Empty;
        public string NombreComuna { get; set; } = string.Empty;
        public int ZonaId { get; set; }
    }

    public class DeliveryService
    {
        // Trae todas las zonas para la pantalla de configuración
        public List<ZonaDelivery> ObtenerZonasActivas()
        {
            using var db = new AppDbContext();
            return db.ZonasDelivery
                .AsNoTracking()
                .Include(z => z.Comunas)
                .OrderBy(z => z.Nombre)
                .ToList();
        }

        public List<ComunaDelivery> ObtenerComunasActivas()
        {
            using var db = new AppDbContext();
            return db.ComunasDelivery
                .AsNoTracking()
                .Include(c => c.Zona)
                .Where(c => c.Activo && c.Zona != null && c.Zona.Activo)
                .OrderBy(c => c.Nombre)
                .ToList();
        }

        public CalculoTarifaDeliveryDTO EvaluarDelivery(int comunaId, decimal subtotalProductos)
        {
            using var db = new AppDbContext();
            var comuna = db.ComunasDelivery
                .AsNoTracking()
                .Include(c => c.Zona)
                .FirstOrDefault(c => c.ComunaDeliveryId == comunaId);

            if (comuna == null || !comuna.Activo || comuna.Zona == null || !comuna.Zona.Activo)
            {
                return new CalculoTarifaDeliveryDTO
                {
                    EsValido = false,
                    MensajeError = "La comuna o zona de cobertura seleccionada no se encuentra activa."
                };
            }

            var zona = comuna.Zona;
            var evaluacion = new CalculoTarifaDeliveryDTO
            {
                ZonaId = zona.ZonaDeliveryId,
                NombreZona = zona.Nombre,
                NombreComuna = comuna.Nombre,
                TiempoEstimadoMinutos = comuna.TiempoEstimadoMinutos
            };

            // 1. Regla de Pedido Mínimo
            if (zona.PedidoMinimo > 0 && subtotalProductos < zona.PedidoMinimo)
            {
                evaluacion.EsValido = false;
                evaluacion.MontoFaltanteMinimo = zona.PedidoMinimo - subtotalProductos;
                evaluacion.MensajeError = $"El pedido mínimo para {zona.Nombre} es de ${zona.PedidoMinimo:N0}. Faltan ${evaluacion.MontoFaltanteMinimo:N0}.";
                return evaluacion;
            }

            evaluacion.EsValido = true;

            // 2. Regla de Envío Gratuito
            if (zona.MontoEnvioGratis > 0 && subtotalProductos >= zona.MontoEnvioGratis)
            {
                evaluacion.CostoFinal = 0m;
                evaluacion.AplicaEnvioGratis = true;
            }
            else
            {
                evaluacion.CostoFinal = zona.CostoEnvio;
                evaluacion.AplicaEnvioGratis = false;
                if (zona.MontoEnvioGratis > 0)
                {
                    evaluacion.MontoFaltanteEnvioGratis = zona.MontoEnvioGratis - subtotalProductos;
                }
            }

            return evaluacion;
        }

        public string GenerarSiguienteNumeroPedido(AppDbContext db)
        {
            int ultimoId = db.Deliveries
                .AsNoTracking()
                .Select(d => d.DeliveryId)
                .DefaultIfEmpty(0)
                .Max();

            int siguiente = ultimoId + 1;
            return $"D-{siguiente:D6}";
        }

        public Delivery RegistrarDeliveryParaVenta(Delivery datosEntrega, int idTve, decimal subtotalProductos, AppDbContext db)
        {
            var evaluacion = EvaluarDelivery(datosEntrega.ComunaDeliveryId, subtotalProductos);
            if (!evaluacion.EsValido)
            {
                throw new InvalidOperationException(evaluacion.MensajeError);
            }

            datosEntrega.VentaId = idTve;
            datosEntrega.NumeroPedidoDelivery = GenerarSiguienteNumeroPedido(db);
            datosEntrega.ZonaDeliveryId = evaluacion.ZonaId;
            datosEntrega.NombreZona = evaluacion.NombreZona;
            datosEntrega.NombreComuna = evaluacion.NombreComuna;
            datosEntrega.CostoDelivery = evaluacion.CostoFinal;
            datosEntrega.TiempoEstimadoMinutos = evaluacion.TiempoEstimadoMinutos;
            
            // TotalVenta congelado (productos + despacho)
            datosEntrega.TotalVenta = subtotalProductos + evaluacion.CostoFinal;
            datosEntrega.EstadoDelivery = EstadoDelivery.Pendiente;
            datosEntrega.FechaCreacion = DateTime.UtcNow;
            datosEntrega.Activo = true;

            db.Deliveries.Add(datosEntrega);
            return datosEntrega;
        }

        public void CambiarEstadoDelivery(int deliveryId, EstadoDelivery nuevoEstado, string? repartidor = null, string? observacion = null)
        {
            using var db = new AppDbContext();
            var delivery = db.Deliveries.Find(deliveryId);
            if (delivery == null) throw new InvalidOperationException("El despacho solicitado no existe.");

            delivery.EstadoDelivery = nuevoEstado;

            if (!string.IsNullOrWhiteSpace(observacion))
            {
                delivery.ObservacionEstado = observacion.Trim();
            }

            switch (nuevoEstado)
            {
                case EstadoDelivery.Preparando:
                    delivery.FechaPreparacion = DateTime.UtcNow;
                    break;
                case EstadoDelivery.EnRuta:
                    delivery.FechaDespacho = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(repartidor)) delivery.Repartidor = repartidor.Trim();
                    break;
                case EstadoDelivery.Entregado:
                    delivery.FechaEntrega = DateTime.UtcNow;
                    break;
            }

            db.SaveChanges();
        }

        public List<Delivery> ObtenerPedidosDelDia(DateTime? fecha = null)
        {
            DateTime dia = (fecha ?? DateTime.UtcNow).Date;
            using var db = new AppDbContext();
            return db.Deliveries
                .AsNoTracking()
                .Include(d => d.Zona)
                .Include(d => d.Comuna)
                .Include(d => d.Venta)
                .Where(d => d.FechaCreacion.Date == dia && d.Activo)
                .OrderByDescending(d => d.FechaCreacion)
                .ToList();
        }

        // --- Mantenimiento de Zonas y Comunas ---
        public void GuardarZona(ZonaDelivery zona)
        {
            using var db = new AppDbContext();
            if (zona.ZonaDeliveryId == 0)
            {
                db.ZonasDelivery.Add(zona);
            }
            else
            {
                var zonaDb = db.ZonasDelivery.Find(zona.ZonaDeliveryId);
                if (zonaDb != null)
                {
                    zonaDb.Nombre = zona.Nombre;
                    zonaDb.CostoEnvio = zona.CostoEnvio;
                    zonaDb.PedidoMinimo = zona.PedidoMinimo;
                    zonaDb.MontoEnvioGratis = zona.MontoEnvioGratis;
                    zonaDb.Activo = zona.Activo;
                }
            }
            db.SaveChanges();
        }

        public void GuardarComuna(ComunaDelivery comuna)
        {
            using var db = new AppDbContext();
            if (comuna.ComunaDeliveryId == 0)
            {
                db.ComunasDelivery.Add(comuna);
            }
            else
            {
                var comunaDb = db.ComunasDelivery.Find(comuna.ComunaDeliveryId);
                if (comunaDb != null)
                {
                    comunaDb.Nombre = comuna.Nombre;
                    comunaDb.ZonaDeliveryId = comuna.ZonaDeliveryId;
                    comunaDb.TiempoEstimadoMinutos = comuna.TiempoEstimadoMinutos;
                    comunaDb.Activo = comuna.Activo;
                }
            }
            db.SaveChanges();
        }
    } // 👈 Llave de cierre de la clase
}     // 👈 Llave de cierre del namespace