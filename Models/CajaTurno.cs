using System;

namespace SISTEMAACTUALIZADO.Models
{
    public class CajaTurno
    {
        public int CajaTurnoID { get; set; }
        public string Usuario { get; set; } = "admin";
        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal MontoEfectivoVentas { get; set; }
        public decimal MontoTarjetaVentas { get; set; }
        public decimal MontoTransferenciaVentas { get; set; }
        public decimal MontoIngresos { get; set; }
        public decimal MontoRetiros { get; set; }
        public decimal? MontoEfectivoReal { get; set; }
        public decimal? Diferencia { get; set; }
        public string Estado { get; set; } = "Abierta"; // Abierta / Cerrada
        public string Observaciones { get; set; } = string.Empty;
    }
}