using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class Proveedor
    {
        public int ProveedorID { get; set; }
        public string Rut { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Giro { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
    }
}