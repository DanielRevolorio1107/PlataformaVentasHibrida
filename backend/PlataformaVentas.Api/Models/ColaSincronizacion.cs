namespace PlataformaVentas.Api.Models;

public class ColaSincronizacion
{
    public Guid Id { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public Guid EntidadId { get; set; }

    public string TipoOperacion { get; set; } = string.Empty;

    public string? Payload { get; set; }

    public string Estado { get; set; } = "PENDIENTE";

    public int Intentos { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaUltimoIntento { get; set; }

    public DateTime? FechaSincronizacion { get; set; }

    public string? UltimoError { get; set; }
}