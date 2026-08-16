using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        public int ProductoID { get; set; }
        public string CodigoBarra { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = "General";
        public decimal PrecioUnitario { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; } = 5;
        public decimal PrecioCosto { get; set; } = 0;
        public decimal MargenGanancia { get; set; } = 30.00m;
        public string? ImagenPath { get; set; }
        public bool Estado { get; set; } = true;

        [NotMapped]
        public bool Activo
        {
            get => Estado;
            set => Estado = value;
        }
    }
}