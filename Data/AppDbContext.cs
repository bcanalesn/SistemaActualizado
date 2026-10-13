using System;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<ConfiguracionMargen> ConfiguracionMargenes { get; set; } = null!;
        public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa { get; set; } = null!;
        public DbSet<PrecioQ> PreciosQ { get; set; } = null!;
        public DbSet<TVE2607> TVE2607 { get; set; } = null!;
        public DbSet<TVD2607> TVD2607 { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Folio> Folios { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;
        public DbSet<Compra> Compras { get; set; } = null!;
        public DbSet<DetalleCompra> DetalleCompras { get; set; } = null!;
        public DbSet<PrecioEspecialCliente> PreciosEspecialesClientes { get; set; } = null!;
        public DbSet<Feriado> Feriados { get; set; } = null!;
        public DbSet<CuentaPorCobrar> CuentasPorCobrar { get; set; } = null!;
        public DbSet<PagoCliente> PagosClientes { get; set; } = null!;
        public DbSet<PagoDetalleFactura> PagosDetalleFacturas { get; set; } = null!;
        public DbSet<HistorialCondicionesCredito> HistorialCondicionesCredito { get; set; } = null!;
        public DbSet<CajaTurno> CajaTurnos { get; set; } = null!;
        public DbSet<ClienteCuentaPuntos> ClienteCuentasPuntos { get; set; } = null!;
        public DbSet<PuntosMovimiento> PuntosMovimientos { get; set; } = null!;

        // Reservas
        public DbSet<Reserva> Reservas { get; set; } = null!;
        public DbSet<ReservaDetalle> ReservaDetalles { get; set; } = null!;
        public DbSet<ReservaPago> ReservaPagos { get; set; } = null!;

        // 👈 Entidades del Módulo de Delivery
        public DbSet<ZonaDelivery> ZonasDelivery { get; set; } = null!;
        public DbSet<ComunaDelivery> ComunasDelivery { get; set; } = null!;
        public DbSet<Delivery> Deliveries { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("Server=localhost;Database=sistemaepos;Uid=root;Pwd=;", 
                    new MySqlServerVersion(new Version(8, 0, 30)));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índice único para correlativo comercial de Delivery (D-XXXXXX)
            modelBuilder.Entity<Delivery>()
                .HasIndex(d => d.NumeroPedidoDelivery)
                .IsUnique()
                .HasDatabaseName("UK_Delivery_NumeroPedido");

            modelBuilder.Entity<PuntosMovimiento>()
                .HasIndex(m => new { m.IdCliente, m.Fecha })
                .HasName("IX_PuntosMovimientos_IdCliente_Fecha");

            modelBuilder.Entity<PuntosMovimiento>()
                .HasIndex(m => m.IdTve)
                .HasName("IX_PuntosMovimientos_IdTve");

            // Relación Zona -> Comunas (evita eliminación accidental en cascada)
            modelBuilder.Entity<ZonaDelivery>()
                .HasMany(z => z.Comunas)
                .WithOne(c => c.Zona)
                .HasForeignKey(c => c.ZonaDeliveryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación TVE2607 (Venta) -> Delivery (1 a 0..1)
            modelBuilder.Entity<TVE2607>()
                .HasOne(v => v.InformacionDelivery)
                .WithOne(d => d.Venta)
                .HasForeignKey<Delivery>(d => d.VentaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relación Reserva -> Delivery (opcional para integración futura)
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Reserva)
                .WithMany()
                .HasForeignKey(d => d.ReservaId)
                .OnDelete(DeleteBehavior.SetNull);
                
        }

        public void AsegurarColumnaEsPesableMySQL()
        {
            try
            {
                using (var connection = this.Database.GetDbConnection())
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT COUNT(*) 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_SCHEMA = DATABASE() 
                              AND TABLE_NAME = 'Productos' 
                              AND COLUMN_NAME = 'EsPesable';";

                        long existe = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);

                        if (existe == 0)
                        {
                            cmd.CommandText = "ALTER TABLE Productos ADD COLUMN EsPesable TINYINT(1) NOT NULL DEFAULT 0;";
                            cmd.ExecuteNonQuery();

                            cmd.CommandText = @"
                                UPDATE Productos 
                                SET EsPesable = 1 
                                WHERE LOWER(Nombre) LIKE '%(gr)%' 
                                   OR LOWER(Nombre) LIKE '%(g)%' 
                                   OR LOWER(Nombre) LIKE '%granel%';";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}