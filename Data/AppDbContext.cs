using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;
        public DbSet<TVE2607> TVE2607 { get; set; } = null!;
        public DbSet<TVD2607> TVD2607 { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Folio> Folios { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = "Server=localhost;Database=sistemaepos;Uid=root;Pwd=;";
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Venta>().ToTable("ventas");

            modelBuilder.Entity<TVE2607>(entity =>
            {
                entity.ToTable("TVE2607");
                entity.HasKey(e => e.idTve);
            });

            modelBuilder.Entity<TVD2607>(entity =>
            {
                entity.ToTable("TVD2607");
                entity.HasKey(d => d.idTvd);
                entity.HasOne(d => d.VentaEncabezado)
                      .WithMany(p => p.Detalles)
                      .HasForeignKey(d => d.idTve);
            });
        }
    }
}