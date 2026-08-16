using System;
using System.ComponentModel.DataAnnotations;

namespace SISTEMAACTUALIZADO.Models
{
    public class Folio
    {
        [Key]
        public int FolioID { get; set; }
        public string TipoDocumento { get; set; } = "Boleta Electrónica";
        public int FolioDesde { get; set; }
        public int FolioHasta { get; set; }
        public int FolioActual { get; set; }
        public bool Activo { get; set; } = true;

        // Propiedades calculadas en tiempo real para métricas SII
        public int FoliosUsados => FolioActual > FolioDesde ? (FolioActual - FolioDesde) : 0;
        public int FoliosDisponibles => FolioHasta >= FolioActual ? (FolioHasta - FolioActual + 1) : 0;
    }
}