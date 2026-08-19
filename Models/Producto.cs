using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("productos")]
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

        public int ListaDefectoPOS { get; set; } = 1;

        [MaxLength(50)]
        public string? NmbCorto { get; set; } = "";

        // Unidades y Factores de Conversión
        [MaxLength(10)]
        public string UniVenta { get; set; } = "UN";

        [Column(TypeName = "decimal(10,2)")]
        public decimal FactorVenta { get; set; } = 1.00m;

        [MaxLength(10)]
        public string UniCosto { get; set; } = "UN";

        [Column(TypeName = "decimal(10,2)")]
        public decimal FactorCompr { get; set; } = 1.00m;

        [Column(TypeName = "decimal(10,3)")]
        public decimal Peso { get; set; } = 0.000m;

        [Column(TypeName = "decimal(10,3)")]
        public decimal FactorPeso { get; set; } = 1.000m;

        // Costos y Margen
        [Column(TypeName = "decimal(18,2)")]
        public decimal PPP { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LstCosto { get; set; } = 0.00m;

        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioCosto { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal MargenGanancia { get; set; } = 30.00m;

        // Listas de Precios (Precio1 es el PrecioUnitario principal de venta)
        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio2 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio3 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio4 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio5 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio6 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio7 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio8 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio9 { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio10 { get; set; } = 0.00m;

        // Categorías y Clasificaciones
        public int IdGrupo { get; set; } = 0;

        [MaxLength(100)]
        public string Categoria { get; set; } = "General";

        public int? IdFamilia { get; set; } = 0;

        [MaxLength(100)]
        public string? NFamilia { get; set; } = "General";

        public string? Categoria1 { get; set; } = "";
        public string? Categoria2 { get; set; } = "";
        public string? Categoria3 { get; set; } = "";

        // Impresión
        public int IdImpresora { get; set; } = 1;

        [MaxLength(100)]
        public string? nmbImpreso { get; set; } = "";

        // Ofertas y Stock
        [Column(TypeName = "decimal(18,2)")]
        public decimal pOferTa { get; set; } = 0.00m;

        public DateTime? FchIni { get; set; }

        public int Stock { get; set; }
        public int StockMinimo { get; set; } = 5;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Qini { get; set; } = 0.00m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Qcompra { get; set; } = 0.00m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Qguia { get; set; } = 0.00m;

        // Auditoría y Estado
        public bool Estado { get; set; } = true;

        [MaxLength(255)]
        public string? ImagenPath { get; set; } = string.Empty;

        public DateTime? FchSincro { get; set; }
        public DateTime? FchUpd { get; set; } = DateTime.Now;
        public byte Sincro { get; set; } = 0;
    }
}