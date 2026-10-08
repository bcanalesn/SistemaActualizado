using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("zonas_delivery")]
    public class ZonaDelivery
    {
        [Key]
        [Column("ZonaDeliveryId")]
        public int ZonaDeliveryId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("CostoEnvio", TypeName = "decimal(12,2)")]
        public decimal CostoEnvio { get; set; }

        [Column("PedidoMinimo", TypeName = "decimal(12,2)")]
        public decimal PedidoMinimo { get; set; }

        [Column("MontoEnvioGratis", TypeName = "decimal(12,2)")]
        public decimal MontoEnvioGratis { get; set; }

        [Column("Activo")]
        public bool Activo { get; set; } = true;

        public virtual ICollection<ComunaDelivery> Comunas { get; set; } = new List<ComunaDelivery>();
    }
}