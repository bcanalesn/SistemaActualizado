using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("configuracion_empresa")]
    public class ConfiguracionEmpresa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int EmpresaID { get; set; } = 1;

        [MaxLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Rut { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Giro { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Direccion { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Comuna { get; set; } = "Santiago";

        [MaxLength(80)]
        public string Ciudad { get; set; } = "Santiago";

        [MaxLength(50)]
        public string Telefono { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string UnidadSII { get; set; } = "S.I.I. - SANTIAGO CENTRO";

        [MaxLength(100)]
        public string ResolucionSII { get; set; } = "Res. 99 de 2026";

        [MaxLength(200)]
        public string TextoPieTicket { get; set; } = "¡Gracias por su compra!";
    }
}