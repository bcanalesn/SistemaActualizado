using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class Categoria
    {
        public int CategoriaID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Icono { get; set; } = "📦";
        public bool Estado { get; set; } = true;
    }
}