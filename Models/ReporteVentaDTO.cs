using System;
using System.Collections.Generic;

namespace SISTEMAACTUALIZADO.Models
{
    public class ResumenReporteVentasDTO
    {
        public decimal TotalRecaudado { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TicketPromedio { get; set; }
        public List<ItemReporteVentaDTO> Ventas { get; set; } = new List<ItemReporteVentaDTO>();
    }

    public class ItemReporteVentaDTO
    {
        public int IdTve { get; set; }
        public int NroDTE { get; set; }
        public DateTime FecDoc { get; set; }
        public string Documento { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string RuT { get; set; } = string.Empty;
        public string MedioPago { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
    }

    public class PrecioEspecialItemDTO
    {
        public int IdEspecial { get; set; }
        public string Producto { get; set; } = string.Empty;
        public decimal CostoBase { get; set; }
        public decimal PrecioEspecial { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string EstadoVigencia { get; set; } = string.Empty;
    }
}