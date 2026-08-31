using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("configuracion_margenes")]
    public class ConfiguracionMargen
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int NumeroLista { get; set; } // 1 a 10

        [Required]
        [MaxLength(100)]
        public string NombreLista { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeMargen { get; set; } = 0.00m;

        public DateTime UltimaModificacion { get; set; } = DateTime.Now;
    }
}