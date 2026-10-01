using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("reserva_detalle")]
    public class ReservaDetalle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReservaDetalle { get; set; } // 👈 Exacto a phpMyAdmin

        public int IdReserva { get; set; }
        public int ProductoID { get; set; }
        public string? CodigoBarra { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public bool EsPesable { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? NotasItem { get; set; }
    }
}