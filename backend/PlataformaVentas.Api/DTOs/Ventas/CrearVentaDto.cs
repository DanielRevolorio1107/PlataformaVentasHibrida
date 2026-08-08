using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Ventas;

public class CrearVentaDto
{
    [Required]
    public Guid MetodoPagoId { get; set; }

    [MaxLength(250)]
    public string? Observaciones { get; set; }

    [Required]
    [MinLength(1)]
    public List<CrearDetalleVentaDto> Detalles { get; set; } = new();
}