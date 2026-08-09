using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Menus;

public class CrearMenuDiarioDto
{
    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    [MinLength(1)]
    public List<CrearMenuDetalleDto> Productos { get; set; } = new();
}