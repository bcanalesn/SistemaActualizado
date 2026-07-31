namespace SISTEMAACTUALIZADO.Models
{
    public class Producto
    {
        public int ProductoID { get; set; }
        public string CodigoBarra { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Stock { get; set; }
        public bool Estado { get; set; } = true;
    }
}