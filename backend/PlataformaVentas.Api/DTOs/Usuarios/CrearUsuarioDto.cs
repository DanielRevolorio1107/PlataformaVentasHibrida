using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Usuarios;

public class CrearUsuarioDto
{
    [Required]
    [MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public Guid RolId { get; set; }
}