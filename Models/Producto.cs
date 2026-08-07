using System;
using System.ComponentModel.DataAnnotations;

namespace SISTEMAACTUALIZADO.Models
{
    public class Producto
    {
        [Key]
        public int ProductoID { get; set; }
        public string? CodigoBarra { get; set; } = string.Empty;
        public string? Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Stock { get; set; }
        public bool Estado { get; set; } = true;
        public string? ImagenPath { get; set; } = string.Empty;
    }
}