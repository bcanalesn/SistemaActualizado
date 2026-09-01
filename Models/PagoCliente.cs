using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("pagos_cliente")]
    public class PagoCliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PagoID { get; set; }

        public int IdCliente { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTotalPago { get; set; }

        [MaxLength(30)]
        public string MedioPago { get; set; } = "EFECTIVO";

        [MaxLength(50)]
        public string? NroComprobante { get; set; }

        [MaxLength(250)]
        public string? Observaciones { get; set; }

        [MaxLength(50)]
        public string UsuarioCobrador { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoFavorGenerado { get; set; } = 0.00m;

        [ForeignKey("IdCliente")]
        public virtual Cliente? Cliente { get; set; }

        public virtual ICollection<PagoDetalleFactura> Detalles { get; set; } = new List<PagoDetalleFactura>();
    }
}