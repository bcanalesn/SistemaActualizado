using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("pos_preferencias")]
    public class PosPreferencia
    {
        [Key]
        public int Id { get; set; } = 1;

        [Required]
        [MaxLength(30)]
        public string ModoOrdenCatalogo { get; set; } = "CATEGORIA"; // CATEGORIA | PERSONALIZADO | MAS_VENDIDOS

        public DateTime? FechaActualizacion { get; set; } = DateTime.Now;
    }
}