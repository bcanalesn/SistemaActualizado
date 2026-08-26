using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("detallecompras")]
    public class DetalleCompra
    {
        [Key]
        [Column("IdDetalleCompra")]
        public int IdDetalleCompra { get; set; }

        public int CompraID { get; set; }
        public string TipoItem { get; set; } = "MERCADERIA"; // MERCADERIA o GASTO
        public int? ProductoID { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string? DescripcionGasto { get; set; }
        public string? CategoriaGasto { get; set; }
        public string? ActivoFijoRef { get; set; }
        public bool AfectaStock { get; set; } = true;
        public int Cantidad { get; set; } = 1;
        public decimal PrecioCostoUnitario { get; set; }
        public decimal PvpSugerido { get; set; }
        public decimal Subtotal { get; set; }

        public Compra? Compra { get; set; }
        public Producto? Producto { get; set; }
    }
}