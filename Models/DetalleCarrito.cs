using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class DetalleCarrito
    {
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }        // Precio real cobrado (con lista/promoción)
        public decimal PrecioLista1 { get; set; }          // Precio base normal de Lista 1
        public int Cantidad { get; set; }
        public decimal Subtotal => PrecioUnitario * Cantidad;       // Lo que paga el cliente
        public decimal SubtotalLista1 => (PrecioLista1 > 0 ? PrecioLista1 : PrecioUnitario) * Cantidad; // Monto sin rebaja
    }
}