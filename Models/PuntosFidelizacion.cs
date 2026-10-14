using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("puntos_cuentas")]
    public class ClienteCuentaPuntos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdCliente { get; set; }

        public int PuntosDisponibles { get; set; } = 0;

        public int PuntosAcumuladosHistorico { get; set; } = 0;

        public int PuntosCanjeadosHistorico { get; set; } = 0;

        public DateTime FechaUltimaActualizacion { get; set; } = DateTime.Now;

        public int VersionFila { get; set; } = 1;
    }

    [Table("puntos_movimientos")]
    public class PuntosMovimiento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdMovimiento { get; set; }

        public int IdCliente { get; set; }

        public int? IdTve { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Tipo { get; set; } = "ACUMULACION"; // ACUMULACION, CANJE, REVERSA, AJUSTE

        public int Puntos { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontoBase { get; set; }

        [MaxLength(255)]
        public string Descripcion { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Usuario { get; set; } = "admin";
    }
}