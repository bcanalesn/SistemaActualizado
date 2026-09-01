using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("cuentas_por_cobrar")]
    public class CuentaPorCobrar
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CxCID { get; set; }

        public int VentaID { get; set; }

        public int IdCliente { get; set; }

        public int TipoDTE { get; set; } = 33;

        public int FolioDoc { get; set; }

        public DateTime FechaEmision { get; set; }

        public int DiasCreditoHabiles { get; set; } = 0;

        [Column(TypeName = "date")]
        public DateTime FechaVencimiento { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoOriginal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoAbonado { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoPendiente { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, PARCIAL, PAGADA, VENCIDA, ANULADA

        [MaxLength(50)]
        public string UsuarioEmisor { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey("IdCliente")]
        public virtual Cliente? Cliente { get; set; }
    }
}