using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("comunas_delivery")]
    public class ComunaDelivery
    {
        [Key]
        [Column("ComunaDeliveryId")]
        public int ComunaDeliveryId { get; set; }

        [Column("ZonaDeliveryId")]
        public int ZonaDeliveryId { get; set; }

        [ForeignKey(nameof(ZonaDeliveryId))]
        public virtual ZonaDelivery? Zona { get; set; }

        [Required]
        [MaxLength(120)]
        [Column("Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("TiempoEstimadoMinutos")]
        public int TiempoEstimadoMinutos { get; set; } = 45;

        [Column("Activo")]
        public bool Activo { get; set; } = true;
    }
}