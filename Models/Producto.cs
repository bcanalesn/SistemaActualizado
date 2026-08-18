using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductoID { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodigoBarra { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        public string? NmbLargo { get; set; } = "";
        public string? NmbCorto { get; set; } = "";

        [MaxLength(100)]
        public string Categoria { get; set; } = "General";

        public int? IdFamilia { get; set; } = 0;

        [MaxLength(100)]
        public string? NFamilia { get; set; } = "General";

        public string? Categoria1 { get; set; } = "";
        public string? Categoria2 { get; set; } = "";
        public string? Categoria3 { get; set; } = "";

        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioUnitario { get; set; }

        public int Stock { get; set; }
        public int StockMinimo { get; set; } = 5;

        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioCosto { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal MargenGanancia { get; set; } = 30.00m;

        public bool Estado { get; set; } = true;

        [MaxLength(255)]
        public string? ImagenPath { get; set; } = string.Empty;
    }
}