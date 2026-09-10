using System;
using System.Collections.Generic;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Models
{
    public class ResumenLibroVentasDTO
    {
        public decimal TotalVentas { get; set; }
        public int CantidadDocumentos { get; set; }
        public int CantidadBoletas { get; set; }
        public decimal PorcentajeBoletas { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal PorcentajeFacturas { get; set; }
        public int CantidadNotasCredito { get; set; }
        public decimal MontoNotasCredito { get; set; }

        public decimal TotalNeto { get; set; }
        public decimal DebitoFiscalTotal { get; set; }

        public List<TVE2607> Ventas { get; set; } = new List<TVE2607>();
    }

    public class ResumenLibroComprasDTO
    {
        public decimal TotalBruto { get; set; }
        public decimal TotalNeto { get; set; }
        public decimal TotalIva { get; set; }
        public int TotalFacturas { get; set; }
        public int TotalProveedores { get; set; }

        public List<Compra> Compras { get; set; } = new List<Compra>();
    }
}