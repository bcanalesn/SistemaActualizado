using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    public class DetalleCompra
    {
        [Key]
        public int IdDetalleCompra { get; set; }
        public int CompraID { get; set; }
        public int ProductoID { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioCostoUnitario { get; set; }
        public decimal Subtotal { get; set; }

        [ForeignKey("CompraID")]
        public Compra? Compra { get; set; }

        [ForeignKey("ProductoID")]
        public Producto? Producto { get; set; }
    }
}