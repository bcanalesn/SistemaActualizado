using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("reserva_pago")]
    public class ReservaPago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReservaPago { get; set; } // 👈 Exacto a phpMyAdmin

        public int IdReserva { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = "Efectivo";
        public int? CajaTurnoID { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public bool EsAbonoInicial { get; set; } = false;
        public bool EsLiquidacionFinal { get; set; } = false;
    }
}