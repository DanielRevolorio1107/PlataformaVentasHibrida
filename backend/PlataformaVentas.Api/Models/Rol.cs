namespace PlataformaVentas.Api.Models;

public class Rol
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}