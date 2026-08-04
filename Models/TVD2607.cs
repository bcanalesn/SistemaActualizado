using System;
using System.ComponentModel.DataAnnotations;

namespace SISTEMAACTUALIZADO.Models
{
    public class TVD2607
    {
        [Key]
        public int idTvd { get; set; }
        public int idTve { get; set; } // Clave foranea hacia TVE2607
        public int idLocal { get; set; } = 1;
        public int iddocDTE { get; set; } = 39;
        public string Documento { get; set; } = "Boleta Electrónica";
        public int NroDTE { get; set; }
        public int NroInT { get; set; }
        public DateTime FecMoV { get; set; } = DateTime.Now;
        public string HoraMoV { get; set; } = DateTime.Now.ToString("HH:mm:ss");
        public int IdProducto { get; set; }
        public string NmbProducto { get; set; } = string.Empty;
        public string nmbCorT { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal PneTo { get; set; }
        public decimal SubTotal { get; set; }
        public decimal SubNeto { get; set; }
        public decimal Costo { get; set; }
        public decimal Tcosto { get; set; }
        public int IdVendedor { get; set; } = 1;
        public string nmbVendedor { get; set; } = "Barbara";
        public int idFamilia { get; set; }
        public string nFamilia { get; set; } = string.Empty;
        public int idGrupo { get; set; }
        public string nGrupo { get; set; } = string.Empty;
        public string Unidad { get; set; } = "UN";

        public TVE2607? VentaEncabezado { get; set; }
    }
}