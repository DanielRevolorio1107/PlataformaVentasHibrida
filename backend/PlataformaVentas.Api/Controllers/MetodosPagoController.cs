using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;

namespace PlataformaVentas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MetodosPagoController : ControllerBase
{
    private readonly AppDbContext _context;

    public MetodosPagoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerMetodosPago()
    {
        var metodos = await _context.MetodosPago
            .Where(m => m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new
            {
                m.Id,
                m.Nombre
            })
            .ToListAsync();

        return Ok(metodos);
    }
}