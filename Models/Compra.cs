using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SISTEMAACTUALIZADO.Models
{
    public class Compra
    {
        [Key]
        public int CompraID { get; set; }
        public string RutProveedor { get; set; } = string.Empty;
        public string RazonSocialProveedor { get; set; } = string.Empty;
        public int NroFacturaProveedor { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public DateTime FechaRecepcion { get; set; } = DateTime.Now;
        public decimal MontoNeto { get; set; }
        public decimal MontoIva { get; set; }
        public decimal MontoTotal { get; set; }
        public string UsuarioReceptor { get; set; } = "Bárbara";
        public string Estado { get; set; } = "Recibida";

        public List<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
    }
}