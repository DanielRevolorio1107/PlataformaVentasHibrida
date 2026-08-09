using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Menus;

public class CrearMenuDetalleDto
{
    [Required]
    public Guid ProductoId { get; set; }
}