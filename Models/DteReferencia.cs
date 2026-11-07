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
        public int? TipoDocModificador { get; set; }
        public string? NombreDocModificador { get; set; }
        public int? FolioModificador { get; set; }

        public int IdTveDTEOrigen { get; set; }
        public int TipoDocOrigen { get; set; }
        public string? NombreDocOrigen { get; set; }
        public int FolioOrigen { get; set; }

        public int CodigoReferencia { get; set; }
        public string? RazonReferencia { get; set; }
        public decimal MontoAfectado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string? Usuario { get; set; }
    }
}