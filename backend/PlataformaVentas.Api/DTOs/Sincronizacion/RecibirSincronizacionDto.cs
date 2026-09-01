using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Sincronizacion;

public class RecibirSincronizacionDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Entidad { get; set; } = string.Empty;

    [Required]
    public Guid EntidadId { get; set; }

    [Required]
    public string TipoOperacion { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;
}