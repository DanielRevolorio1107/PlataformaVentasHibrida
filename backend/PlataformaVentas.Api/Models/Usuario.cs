namespace PlataformaVentas.Api.Models;

public class Usuario
{
    public Guid Id { get; set; }

    public Guid RolId { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string NombreUsuario { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public Rol Rol { get; set; } = null!;

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}