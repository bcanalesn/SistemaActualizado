using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("reserva")]
    public class Reserva
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReserva { get; set; }

        public string CodigoReserva { get; set; } = string.Empty;
        public int? IdCliente { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaEntregaPactada { get; set; }
        public decimal TotalPedido { get; set; }
        public decimal TotalAbonado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "Reservado";
        public string? Observaciones { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public string? UsuarioEntrega { get; set; }
        public DateTime? FechaEntregaReal { get; set; }
        public int? IdDTEGenerado { get; set; }
        public bool StockDescontado { get; set; } = false;

        // Propiedades auxiliares de lectura
        [NotMapped]
        public string NombreCliente { get; set; } = string.Empty;

        [NotMapped]
        public string TelefonoCliente { get; set; } = string.Empty;

        [NotMapped]
        public string RutCliente { get; set; } = string.Empty;

        [NotMapped]
        public List<ReservaDetalle> Detalles { get; set; } = new List<ReservaDetalle>();

        [NotMapped]
        public List<ReservaPago> Pagos { get; set; } = new List<ReservaPago>();
    }
}