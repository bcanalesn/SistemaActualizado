using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("pos_categorias")]
    public class PosCategoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public int Orden { get; set; } = 1;

        [MaxLength(10)]
        public string FondoHex { get; set; } = "#F0FDF4";

        [MaxLength(10)]
        public string TextoHex { get; set; } = "#16A34A";

        [MaxLength(10)]
        public string BordeHex { get; set; } = "#15803D";

        public bool Activo { get; set; } = true;

        public virtual ICollection<PosFamilia> Familias { get; set; } = new List<PosFamilia>();
    }
}