using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("dte_referencias")]
    public class DteReferencia
    {
        [Key]
        public int IdReferencia { get; set; }

        public int IdTveDTEModificador { get; set; }
        public int IdTveDTEOrigen { get; set; }
        public int FolioOrigen { get; set; }
        public int TipoDocOrigen { get; set; }
        public int CodigoReferencia { get; set; } = 1;
        public string RazonReferencia { get; set; } = string.Empty;
        public decimal MontoAfectado { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string Usuario { get; set; } = "admin";

        [ForeignKey("IdTveDTEModificador")]
        public virtual TVE2607? DocumentoModificador { get; set; }

        [ForeignKey("IdTveDTEOrigen")]
        public virtual TVE2607? DocumentoOrigen { get; set; }
    }
}