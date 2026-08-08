using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Productos;

public class EditarProductoDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Range(0.01, 999999.99)]
    public decimal Precio { get; set; }

    public bool Activo { get; set; }
}