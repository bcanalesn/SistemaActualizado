using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = "Cajero"; // Administrador / Cajero
        public int HorasTurno { get; set; } = 9;
        public bool Estado { get; set; } = true;
    }
}