using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("feriados")]
    public class Feriado
    {
        [Key]
        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Required]
        [MaxLength(100)]
        public string Descripcion { get; set; } = string.Empty;

        public bool EsIrrenunciable { get; set; } = false;
    }
}