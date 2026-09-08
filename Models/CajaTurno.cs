using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("caja_turnos")]
    public class CajaTurno
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CajaTurnoID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Usuario { get; set; } = "admin";

        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal MontoInicial { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal MontoEfectivoVentas { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal MontoTarjetaVentas { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal MontoTransferenciaVentas { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal MontoCreditoComercial { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal? MontoEfectivoReal { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal? Diferencia { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "Abierta"; // Abierta / Cerrada

        public string? Observaciones { get; set; }
    }
}