namespace SISTEMAACTUALIZADO.Models
{
    public class VentaDetalle
    {
        public int VentaDetalleID { get; set; }
        public int VentaID { get; set; }
        public int ProductoID { get; set; }
        public string CodigoBarra { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }

        public Venta Venta { get; set; } = null!;
    }
}