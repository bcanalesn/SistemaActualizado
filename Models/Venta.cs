using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class Venta
    {
        public int VentaID { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public string MedioPago { get; set; } = "Efectivo";
        public string Usuario { get; set; } = "barbara";
    }
}