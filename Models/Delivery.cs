using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SISTEMAACTUALIZADO.Models.Enums;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("deliveries")]
    public class Delivery
    {
        [Key]
        [Column("DeliveryId")]
        public int DeliveryId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("NumeroPedidoDelivery")]
        public string NumeroPedidoDelivery { get; set; } = string.Empty; // Ej: D-000001

        [Column("VentaId")]
        public int? VentaId { get; set; }

        // Vínculo con TVE2607 (Encabezado de Venta real del sistema)
        [ForeignKey(nameof(VentaId))]
        public virtual TVE2607? Venta { get; set; }

        [Column("ReservaId")]
        public int? ReservaId { get; set; }

        [ForeignKey(nameof(ReservaId))]
        public virtual Reserva? Reserva { get; set; }

        [Column("ZonaDeliveryId")]
        public int ZonaDeliveryId { get; set; }

        [ForeignKey(nameof(ZonaDeliveryId))]
        public virtual ZonaDelivery? Zona { get; set; }

        [Column("ComunaDeliveryId")]
        public int ComunaDeliveryId { get; set; }

        [ForeignKey(nameof(ComunaDeliveryId))]
        public virtual ComunaDelivery? Comuna { get; set; }

        // --- Históricos Congelados ---
        [Required]
        [MaxLength(100)]
        [Column("NombreZona")]
        public string NombreZona { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("NombreComuna")]
        public string NombreComuna { get; set; } = string.Empty;

        // --- Datos de Contacto y Dirección ---
        [Required]
        [MaxLength(150)]
        [Column("NombreRecibe")]
        public string NombreRecibe { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("Telefono")]
        public string Telefono { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("Direccion")]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("Numero")]
        public string Numero { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("Departamento")]
        public string? Departamento { get; set; }

        [MaxLength(255)]
        [Column("Referencia")]
        public string? Referencia { get; set; }

        [MaxLength(255)]
        [Column("Instrucciones")]
        public string? Instrucciones { get; set; }

        // --- Valores Financieros y Tiempos Congelados ---
        [Column("CostoDelivery", TypeName = "decimal(12,2)")]
        public decimal CostoDelivery { get; set; }

        [Column("TotalVenta", TypeName = "decimal(12,2)")]
        public decimal TotalVenta { get; set; } // Cobro final consolidado (Productos + Delivery - Descuento)

        [Column("TiempoEstimadoMinutos")]
        public int TiempoEstimadoMinutos { get; set; }

        // --- Estados y Auditoría ---
        [Column("EstadoDelivery")]
        public EstadoDelivery EstadoDelivery { get; set; } = EstadoDelivery.Pendiente;

        [MaxLength(255)]
        [Column("ObservacionEstado")]
        public string? ObservacionEstado { get; set; } // Incidencias: cliente ausente, timbre malo, etc.

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("FechaPreparacion")]
        public DateTime? FechaPreparacion { get; set; }

        [Column("FechaDespacho")]
        public DateTime? FechaDespacho { get; set; }

        [Column("FechaEntrega")]
        public DateTime? FechaEntrega { get; set; }

        [MaxLength(120)]
        [Column("Repartidor")]
        public string? Repartidor { get; set; }

        [Column("Activo")]
        public bool Activo { get; set; } = true;
    }
}