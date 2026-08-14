using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SISTEMAACTUALIZADO.Models
{
    public class TVE2607
    {
        [Key]
        public int idTve { get; set; }
        public int idLocal { get; set; } = 1;
        public string nmbLocal { get; set; } = "Local Principal";
        public int iddocDTE { get; set; } = 39; // 39 Boleta, 33 Factura, 61 Nota Credito
        public string Documento { get; set; } = "Boleta Electrónica";
        public int nroDTE { get; set; }
        public int nroInT { get; set; }
        public DateTime FecDoc { get; set; } = DateTime.Now;
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Neto { get; set; }
        public decimal Impto1 { get; set; }
        public decimal Impto2 { get; set; }
        public decimal Impto3 { get; set; }
        public decimal IvA { get; set; }
        public decimal Total { get; set; }
        public string UserDTE { get; set; } = "barbara";
        public string Vendedor { get; set; } = "Barbara";
        public int nroZ { get; set; }
        public string Url { get; set; } = string.Empty;
        public int nPAX { get; set; } = 1;
        public int Idcliente { get; set; }
        public string DNI { get; set; } = string.Empty;
        public string RuT { get; set; } = string.Empty;
        public string dv { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Giro { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int idcomuna { get; set; }
        public string nComuna { get; set; } = string.Empty;
        public int idCiudad { get; set; }
        public string nCiudad { get; set; } = string.Empty;
        public string Fono1 { get; set; } = string.Empty;
        public string Fono2 { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string status { get; set; } = "1";
        public int? idREF { get; set; }
        public int? nroREF { get; set; }
        public string? codigoREF { get; set; }
        public DateTime? FechaREF { get; set; }
        public string HoraDoc { get; set; } = DateTime.Now.ToString("HH:mm:ss");
        public string MedioPago { get; set; } = "Efectivo";
        public decimal Vuelto { get; set; } = 0;

        public List<TVD2607> Detalles { get; set; } = new List<TVD2607>();
    }
}