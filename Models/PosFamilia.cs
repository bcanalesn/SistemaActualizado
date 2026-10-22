using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("pos_familias")]
    public class PosFamilia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public int Orden { get; set; } = 1;

        public bool Activo { get; set; } = true;

        [ForeignKey("CategoriaId")]
        public virtual PosCategoria? Categoria { get; set; }
    }
}