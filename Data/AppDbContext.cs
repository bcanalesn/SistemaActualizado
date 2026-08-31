using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<ConfiguracionMargen> ConfiguracionMargenes { get; set; } = null!;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("Server=localhost;Database=sistemaepos;Uid=root;Pwd=root;", 
                    new MySqlServerVersion(new Version(8, 0, 30)));
            }
        }
    }
}