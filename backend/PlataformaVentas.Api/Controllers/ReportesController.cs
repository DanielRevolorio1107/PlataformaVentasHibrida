using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;

namespace PlataformaVentas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("ventas-hoy")]
    public async Task<IActionResult> ObtenerVentasHoy()
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        var ventas = await _context.Ventas
            .Where(v =>
                v.FechaVenta >= hoy &&
                v.FechaVenta < manana &&
                v.Estado == "REGISTRADA")
            .ToListAsync();

        var totalVentas = ventas.Count;
        var totalIngresos = ventas.Sum(v => v.Total);

        return Ok(new
        {
            fecha = hoy.ToString("yyyy-MM-dd"),
            totalVentas,
            totalIngresos
        });
    }

    [HttpGet("ingresos-metodo-pago")]
    public async Task<IActionResult> ObtenerIngresosPorMetodoPago()
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        var resumen = await _context.MetodosPago
            .Where(m => m.Activo)
            .Select(m => new
            {
                metodoPago = m.Nombre,

                cantidadVentas = m.Ventas.Count(v =>
                    v.FechaVenta >= hoy &&
                    v.FechaVenta < manana &&
                    v.Estado == "REGISTRADA"),

                totalIngresos = m.Ventas
                    .Where(v =>
                        v.FechaVenta >= hoy &&
                        v.FechaVenta < manana &&
                        v.Estado == "REGISTRADA")
                    .Sum(v => (decimal?)v.Total) ?? 0
            })
            .OrderBy(m => m.metodoPago)
            .ToListAsync();

        return Ok(new
        {
            fecha = hoy.ToString("yyyy-MM-dd"),
            resumen
        });
    }

    [HttpGet("ventas-por-fecha")]
    public async Task<IActionResult> ObtenerVentasPorFecha(DateTime? fecha)
    {
        if (fecha == null)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar una fecha."
            });
        }

        var inicio = fecha.Value.Date;
        var fin = inicio.AddDays(1);

        var ventas = await _context.Ventas
            .Where(v =>
                v.FechaVenta >= inicio &&
                v.FechaVenta < fin)
            .Include(v => v.Usuario)
            .Include(v => v.MetodoPago)
            .OrderByDescending(v => v.FechaVenta)
            .Select(v => new
            {
                v.Id,
                v.FechaVenta,
                v.Total,
                v.Estado,
                Usuario = v.Usuario.NombreCompleto,
                MetodoPago = v.MetodoPago.Nombre
            })
            .ToListAsync();

        return Ok(new
        {
            fecha = inicio.ToString("yyyy-MM-dd"),
            cantidadVentas = ventas.Count,
            ventas
        });
    }

    [HttpGet("productos-mas-vendidos")]
    public async Task<IActionResult> ObtenerProductosMasVendidos()
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        var productos = await _context.DetalleVentas
            .Where(d =>
                d.Venta.FechaVenta >= hoy &&
                d.Venta.FechaVenta < manana &&
                d.Venta.Estado == "REGISTRADA")
            .GroupBy(d => new
            {
                d.ProductoId,
                d.Producto.Nombre
            })
            .Select(g => new
            {
                productoId = g.Key.ProductoId,
                producto = g.Key.Nombre,
                cantidadVendida = g.Sum(d => d.Cantidad),
                totalGenerado = g.Sum(d => d.Subtotal)
            })
            .OrderByDescending(p => p.cantidadVendida)
            .ToListAsync();

        return Ok(new
        {
            fecha = hoy.ToString("yyyy-MM-dd"),
            productos
        });
    }
}