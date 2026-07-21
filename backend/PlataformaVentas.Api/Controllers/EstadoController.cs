using Microsoft.AspNetCore.Mvc;

namespace PlataformaVentas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstadoController : ControllerBase
{
    [HttpGet]
    public IActionResult ObtenerEstado()
    {
        return Ok(new
        {
            mensaje = "La API de Plataforma Ventas está funcionando",
            estado = "Activo",
            fecha = DateTime.Now
        });
    }
}