using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Column("Idcliente")]
        public int IdCliente { get; set; }

        public string Rut { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Giro { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Comuna { get; set; } = "SANTIAGO";
        public string Ciudad { get; set; } = "SANTIAGO";
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FormaPago { get; set; } = "CONTADO";
        public int DiasCredito { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CupoCredito { get; set; } = 0;
        
        public int ListaPrecioDefecto { get; set; } = 1;
        public string CategoriaCliente { get; set; } = "MINORISTA";
        public bool Estado { get; set; } = true;

        // --- NUEVOS CAMPOS PARA CRÉDITO COMERCIAL ---

        public bool PermiteCredito { get; set; } = false;

        [MaxLength(20)]
        public string ModalidadPago { get; set; } = "CONTADO_CREDITO"; // SOLO_CONTADO, SOLO_CREDITO, CONTADO_CREDITO

        [MaxLength(20)]
        public string EstadoCrediticio { get; set; } = "ACTIVO"; // ACTIVO, BLOQUEADO, SUSPENDIDO, MOROSO

        public int DiasCreditoHabiles { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoUtilizado { get; set; } = 0.00m;
    }
}