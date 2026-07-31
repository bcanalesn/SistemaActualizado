using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class Cliente
    {
        public int ClienteID { get; set; }
        public string Rut { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
    }
}