using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Models;

namespace PlataformaVentas.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<MetodoPago> MetodosPago { get; set; }
    public DbSet<Venta> Ventas { get; set; }
    public DbSet<DetalleVenta> DetalleVentas { get; set; }
    public DbSet<MenuDiario> MenusDiarios { get; set; }
    public DbSet<MenuDetalle> MenuDetalles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Rol>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<Usuario>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<Producto>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<MetodoPago>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<Venta>()
            .HasKey(v => v.Id);

        modelBuilder.Entity<DetalleVenta>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<Producto>()
            .Property(p => p.Precio)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Venta>()
            .Property(v => v.Total)
            .HasPrecision(12, 2);

        modelBuilder.Entity<DetalleVenta>()
            .Property(d => d.PrecioUnitario)
            .HasPrecision(10, 2);

        modelBuilder.Entity<DetalleVenta>()
            .Property(d => d.Subtotal)
            .HasPrecision(12, 2);

        modelBuilder.Entity<MenuDiario>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<MenuDetalle>()
            .HasIndex(m => new { m.MenuDiarioId, m.ProductoId })
            .IsUnique();
    }
}