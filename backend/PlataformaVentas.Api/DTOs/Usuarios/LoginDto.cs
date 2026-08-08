using System.ComponentModel.DataAnnotations;

namespace PlataformaVentas.Api.DTOs.Usuarios;

public class LoginDto
{
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}