using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("pago_detalle_facturas")]
    public class PagoDetalleFactura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DetallePagoID { get; set; }

        public int PagoID { get; set; }

        public int CxCID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoAplicado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoAnteriorFactura { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoPosteriorFactura { get; set; }

        [ForeignKey("PagoID")]
        public virtual PagoCliente? Pago { get; set; }

        [ForeignKey("CxCID")]
        public virtual CuentaPorCobrar? CuentaPorCobrar { get; set; }
    }
}