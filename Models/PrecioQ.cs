using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("PrecioQ")]
    public class PrecioQ
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPQ { get; set; }

        public int IdProducto { get; set; }
        public string? NProducto { get; set; } = "";

        [Column(TypeName = "decimal(18,3)")]
        public decimal Qini { get; set; } = 1;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Qfin { get; set; } = 999999;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NPrecio { get; set; } = 0;

        public int Bloqueo { get; set; } = 0;
        public DateTime? FchMod { get; set; } = DateTime.Now;
        public string? HoraMod { get; set; } = "";
        public string? IdPrecio { get; set; } = "1";
    }
}