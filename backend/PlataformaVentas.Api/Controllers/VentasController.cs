using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;
using PlataformaVentas.Api.DTOs.Ventas;
using PlataformaVentas.Api.Models;
using System.Security.Claims;

namespace PlataformaVentas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly AppDbContext _context;

    public VentasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CrearVenta(CrearVentaDto dto)
    {
        // Obtener el usuario directamente del JWT
        var usuarioIdTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(usuarioIdTexto, out Guid usuarioId))
        {
            return Unauthorized(new
            {
                mensaje = "No se pudo identificar al usuario."
            });
        }

        // Verificar método de pago
        var metodoPagoExiste = await _context.MetodosPago
            .AnyAsync(m => m.Id == dto.MetodoPagoId && m.Activo);

        if (!metodoPagoExiste)
        {
            return BadRequest(new
            {
                mensaje = "El método de pago no existe o está inactivo."
            });
        }

        if (dto.Detalles == null || dto.Detalles.Count == 0)
        {
            return BadRequest(new
            {
                mensaje = "La venta debe contener al menos un producto."
            });
        }
        var hoy = DateTime.Today;

        var menuHoy = await _context.MenusDiarios
            .Include(m => m.Detalles)
            .FirstOrDefaultAsync(m =>
                m.Fecha == hoy &&
                m.Activo);

        if (menuHoy == null)
        {
            return BadRequest(new
            {
                mensaje = "No existe un menú activo para hoy."
            });
        }

        var productosRepetidos = dto.Detalles
            .GroupBy(d => d.ProductoId)
            .Any(g => g.Count() > 1);

        if (productosRepetidos)
        {
            return BadRequest(new
            {
                mensaje = "No puede repetir el mismo producto en la venta."
            });
        }

        var productosDisponibles = menuHoy.Detalles
            .Where(d => d.Disponible)
            .Select(d => d.ProductoId)
            .ToHashSet();

        foreach (var item in dto.Detalles)
        {
            if (!productosDisponibles.Contains(item.ProductoId))
            {
                return BadRequest(new
                {
                    mensaje = $"El producto {item.ProductoId} no está disponible en el menú de hoy."
                });
            }
        }

        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            MetodoPagoId = dto.MetodoPagoId,
            FechaVenta = DateTime.Now,
            Estado = "REGISTRADA",
            Observaciones = dto.Observaciones?.Trim(),
            Sincronizado = false
        };

        decimal total = 0;

        foreach (var item in dto.Detalles)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.Id == item.ProductoId &&
                    p.Activo);

            if (producto == null)
            {
                return BadRequest(new
                {
                    mensaje = $"El producto {item.ProductoId} no existe o está inactivo."
                });
            }

            var subtotal = producto.Precio * item.Cantidad;

            var detalle = new DetalleVenta
            {
                Id = Guid.NewGuid(),
                VentaId = venta.Id,
                ProductoId = producto.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = producto.Precio,
                Subtotal = subtotal
            };

            venta.Detalles.Add(detalle);

            total += subtotal;
        }

        venta.Total = total;

        _context.Ventas.Add(venta);

        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            mensaje = "Venta registrada correctamente",
            venta.Id,
            venta.FechaVenta,
            venta.Total,
            venta.MetodoPagoId,
            venta.UsuarioId,
            venta.Estado
        });
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerVentas()
    {
        var ventas = await _context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.MetodoPago)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .OrderByDescending(v => v.FechaVenta)
            .Select(v => new
            {
                v.Id,
                v.FechaVenta,
                v.Total,
                v.Estado,
                v.Observaciones,
                Usuario = v.Usuario.NombreCompleto,
                MetodoPago = v.MetodoPago.Nombre,
                Detalles = v.Detalles.Select(d => new
                {
                    Producto = d.Producto.Nombre,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal
                })
            })
            .ToListAsync();

        return Ok(ventas);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerVentaPorId(Guid id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.MetodoPago)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(v => v.Id == id)
            .Select(v => new
            {
                v.Id,
                v.FechaVenta,
                v.Total,
                v.Estado,
                v.Observaciones,
                Usuario = v.Usuario.NombreCompleto,
                MetodoPago = v.MetodoPago.Nombre,
                Detalles = v.Detalles.Select(d => new
                {
                    Producto = d.Producto.Nombre,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal
                })
            })
            .FirstOrDefaultAsync();

        if (venta == null)
        {
            return NotFound(new
            {
                mensaje = "Venta no encontrada"
            });
        }

        return Ok(venta);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/anular")]
    public async Task<IActionResult> AnularVenta(Guid id)
    {
        var venta = await _context.Ventas.FindAsync(id);

        if (venta == null)
        {
            return NotFound(new
            {
                mensaje = "Venta no encontrada"
            });
        }

        if (venta.Estado == "ANULADA")
        {
            return Conflict(new
            {
                mensaje = "La venta ya se encuentra anulada"
            });
        }

        venta.Estado = "ANULADA";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Venta anulada correctamente",
            venta.Id,
            venta.Estado
        });
    }
}