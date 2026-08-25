using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SISTEMAACTUALIZADO.Models
{
    [Table("precios_especiales_cliente")]
    public class PrecioEspecialCliente
    {
        [Key]
        [Column("id_especial")]
        public int IdEspecial { get; set; }

        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [Column("producto_id")]
        public int ProductoId { get; set; }

        [Column("precio_especial")]
        public decimal PrecioEspecial { get; set; }

        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateTime FechaFin { get; set; }

        [Column("estado")]
        public bool Estado { get; set; } = true;

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}