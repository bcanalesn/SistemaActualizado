using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class DetalleCompra
    {
        public int DetalleCompraID { get; set; }
        public int CompraID { get; set; }
        public int ProductoID { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioCostoUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioCostoUnitario;
    }
}