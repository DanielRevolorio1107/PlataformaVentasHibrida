using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Ventas;

public class CrearDetalleVentaDto
{
    [Required]
    public Guid ProductoId { get; set; }

    [Range(1, 999)]
    public int Cantidad { get; set; }
}