using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class Compra
    {
        public int CompraID { get; set; }
        public int ProveedorID { get; set; }
        public string RutProveedor { get; set; } = string.Empty;
        public string RazonSocialProveedor { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = "Factura de Compra"; // Factura de Compra / Guía de Recepción
        public int FolioDocumento { get; set; }
        public DateTime FechaRecepcion { get; set; } = DateTime.Now;
        public decimal TotalNeto { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }
        public string Usuario { get; set; } = "barbara";
    }
}
