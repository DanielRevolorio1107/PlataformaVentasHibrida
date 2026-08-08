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
}