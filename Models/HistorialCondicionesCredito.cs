using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("historial_condiciones_credito")]
    public class HistorialCondicionesCredito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HistorialID { get; set; }

        public int IdCliente { get; set; }

        public int DiasCreditoAnterior { get; set; }

        public int DiasCreditoNuevo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CupoAnterior { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CupoNuevo { get; set; }

        [MaxLength(20)]
        public string EstadoAnterior { get; set; } = "ACTIVO";

        [MaxLength(20)]
        public string EstadoNuevo { get; set; } = "ACTIVO";

        [MaxLength(250)]
        public string? Motivo { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string UsuarioResponsable { get; set; } = string.Empty;

        // Relación con Cliente
        [ForeignKey("IdCliente")]
        public virtual Cliente? Cliente { get; set; }
    }
}