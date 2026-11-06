using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("configuracion_dte")]
    public class ConfiguracionDte
    {
        [Key]
        public int IdConfig { get; set; } = 1;

        public int MaxBoletasPorNC { get; set; } = 10;

        public bool PermitirNCParcial { get; set; } = true;

        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string UsuarioModifica { get; set; } = "admin";
    }
}