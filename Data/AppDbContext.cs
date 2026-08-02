using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Folio> Folios { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = "Server=localhost;Database=sistemaepos;Uid=root;Pwd=;";
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }
    }
}