using System;
using System.Collections.Generic;

namespace SISTEMAACTUALIZADO.Models
{
    public class Venta
    {
        public int VentaID { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public decimal Neto { get; set; }
        public decimal IVA { get; set; }
        public string MedioPago { get; set; } = "Efectivo";
        public string Usuario { get; set; } = "barbara";

        // Campos tributarios DTE (SII Chile)
        public string TipoDocumento { get; set; } = "Boleta Electrónica"; // Boleta Electrónica, Factura Electrónica, Guía de Despacho
        public int FolioDTE { get; set; }
        public string RutCliente { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Giro { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Comuna { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string EstadoDTE { get; set; } = "Aceptado_SII";

        // Referencias para Notas de Crédito / Anulaciones
        public int? idREF { get; set; }
        public int? nroREF { get; set; }
        public string? codigoREF { get; set; }
        public string? GlosaREF { get; set; }

        public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    }
}