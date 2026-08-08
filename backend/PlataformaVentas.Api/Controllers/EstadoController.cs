using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;

namespace PlataformaVentas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstadoController : ControllerBase
{
    private readonly AppDbContext _context;

    public EstadoController(AppDbContext context)
    {
        _context = context;
    }

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

    [HttpGet("base-datos")]
    public async Task<IActionResult> ProbarBaseDatos()
    {
        var roles = await _context.Roles
            .Select(r => new
            {
                r.Id,
                r.Nombre
            })
            .ToListAsync();

        return Ok(new
        {
            mensaje = "Conexión con SQL Server correcta",
            roles
        });
    }
}