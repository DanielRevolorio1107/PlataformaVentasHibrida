namespace PlataformaVentas.Api.Models;

public class MenuDiario
{
    public Guid Id { get; set; }

    public DateTime Fecha { get; set; }

    public Guid UsuarioId { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public ICollection<MenuDetalle> Detalles { get; set; } = new List<MenuDetalle>();
}