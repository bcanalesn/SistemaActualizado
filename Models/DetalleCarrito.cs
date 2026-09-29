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

        // Identificación real y tipada del tipo de venta:
        public bool EsPesable { get; set; } = false;

        public decimal Subtotal => PrecioUnitario * Cantidad;       // Lo que paga el cliente
        public decimal SubtotalLista1 => (PrecioLista1 > 0 ? PrecioLista1 : PrecioUnitario) * Cantidad; // Monto sin rebaja

        // Propiedades de conveniencia para UI y Tickets
        public string UnidadMedida => EsPesable ? "g" : "un.";
        public string CantidadTexto => EsPesable ? $"{Cantidad} g" : $"{Cantidad} un.";
    }
}