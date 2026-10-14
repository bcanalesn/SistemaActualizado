using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("puntos_configuracion")]
    public class PuntosConfiguracion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdConfig { get; set; } = 1;

        public bool Activo { get; set; } = true;

        public int MontoBasePorPunto { get; set; } = 1000;

        public int PuntosPorBloque { get; set; } = 1;

        public int MontoMinimoAcumulacion { get; set; } = 1000;

        public decimal ValorPesoPorPunto { get; set; } = 10.00m;

        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string UsuarioModifica { get; set; } = "admin";
    }
}