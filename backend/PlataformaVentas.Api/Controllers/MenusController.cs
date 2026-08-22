using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;
using PlataformaVentas.Api.DTOs.Menus;
using PlataformaVentas.Api.Models;
using System.Security.Claims;

namespace PlataformaVentas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly AppDbContext _context;

    public MenusController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> CrearMenu(CrearMenuDiarioDto dto)
    {
        var usuarioIdTexto =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(usuarioIdTexto, out Guid usuarioId))
        {
            return Unauthorized(new
            {
                mensaje = "No se pudo identificar al usuario."
            });
        }

        var fechaMenu = dto.Fecha.Date;

        var menuExiste = await _context.MenusDiarios
            .AnyAsync(m => m.Fecha == fechaMenu);

        if (menuExiste)
        {
            return Conflict(new
            {
                mensaje = "Ya existe un menú para esta fecha."
            });
        }

        if (dto.Productos == null || dto.Productos.Count == 0)
        {
            return BadRequest(new
            {
                mensaje = "El menú debe contener al menos un producto."
            });
        }

        var productosRepetidos = dto.Productos
            .GroupBy(p => p.ProductoId)
            .Any(g => g.Count() > 1);

        if (productosRepetidos)
        {
            return BadRequest(new
            {
                mensaje = "No puede agregar el mismo producto más de una vez."
            });
        }

        var idsProductos = dto.Productos
            .Select(p => p.ProductoId)
            .ToList();

        var productosValidos = await _context.Productos
            .Where(p =>
                idsProductos.Contains(p.Id) &&
                p.Activo)
            .ToListAsync();

        if (productosValidos.Count != idsProductos.Count)
        {
            return BadRequest(new
            {
                mensaje = "Uno o más productos no existen o están inactivos."
            });
        }

        var menu = new MenuDiario
        {
            Id = Guid.NewGuid(),
            Fecha = fechaMenu,
            UsuarioId = usuarioId,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        foreach (var producto in productosValidos)
        {
            menu.Detalles.Add(new MenuDetalle
            {
                Id = Guid.NewGuid(),
                MenuDiarioId = menu.Id,
                ProductoId = producto.Id,
                Disponible = true
            });
        }

        _context.MenusDiarios.Add(menu);

        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            mensaje = "Menú diario creado correctamente",
            menu.Id,
            menu.Fecha,
            menu.Activo,
            cantidadProductos = menu.Detalles.Count
        });
    }

    [HttpGet("hoy")]
    public async Task<IActionResult> ObtenerMenuHoy()
    {
        var hoy = DateTime.Today;

        var menu = await _context.MenusDiarios
            .Where(m => m.Fecha == hoy && m.Activo)
            .Include(m => m.Detalles)
                .ThenInclude(d => d.Producto)
            .Select(m => new
            {
                m.Id,
                m.Fecha,
                Productos = m.Detalles.Select(d => new
                {
                    d.ProductoId,
                    d.Producto.Nombre,
                    d.Producto.Descripcion,
                    d.Producto.Precio,
                    d.Disponible
                })
            })
            .FirstOrDefaultAsync();

        if (menu == null)
        {
            return NotFound(new
            {
                mensaje = "No existe un menú activo para hoy."
            });
        }

        return Ok(menu);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{menuId:guid}/productos/{productoId:guid}/disponibilidad")]
    public async Task<IActionResult> CambiarDisponibilidad(
    Guid menuId,
    Guid productoId,
    bool disponible)
    {
        var detalle = await _context.MenuDetalles
            .FirstOrDefaultAsync(d =>
                d.MenuDiarioId == menuId &&
                d.ProductoId == productoId);

        if (detalle == null)
        {
            return NotFound(new
            {
                mensaje = "El producto no pertenece a este menú."
            });
        }

        detalle.Disponible = disponible;

        var menu = await _context.MenusDiarios.FindAsync(menuId);

        if (menu != null)
        {
            menu.FechaActualizacion = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = disponible
                ? "Producto disponible nuevamente."
                : "Producto marcado como agotado.",
            productoId,
            disponible
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{menuId:guid}/productos/{productoId:guid}")]
    public async Task<IActionResult> AgregarProducto(
    Guid menuId,
    Guid productoId)
    {
        var menu = await _context.MenusDiarios
            .FirstOrDefaultAsync(m =>
                m.Id == menuId &&
                m.Activo);

        if (menu == null)
        {
            return NotFound(new
            {
                mensaje = "Menú no encontrado."
            });
        }

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p =>
                p.Id == productoId &&
                p.Activo);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado o inactivo."
            });
        }

        var detalleExistente = await _context.MenuDetalles
            .FirstOrDefaultAsync(d =>
                d.MenuDiarioId == menuId &&
                d.ProductoId == productoId);

        if (detalleExistente != null)
        {
            
            detalleExistente.Disponible = true;

            menu.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "El producto ya pertenecía al menú y fue habilitado nuevamente."
            });
        }

        var detalle = new MenuDetalle
        {
            Id = Guid.NewGuid(),
            MenuDiarioId = menuId,
            ProductoId = productoId,
            Disponible = true
        };

        _context.MenuDetalles.Add(detalle);

        menu.FechaActualizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto agregado al menú correctamente.",
            productoId = producto.Id,
            producto = producto.Nombre
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{menuId:guid}/productos/{productoId:guid}")]
    public async Task<IActionResult> QuitarProducto(
    Guid menuId,
    Guid productoId)
    {
        var menu = await _context.MenusDiarios
            .FirstOrDefaultAsync(m =>
                m.Id == menuId &&
                m.Activo);

        if (menu == null)
        {
            return NotFound(new
            {
                mensaje = "Menú no encontrado."
            });
        }

        var detalle = await _context.MenuDetalles
            .FirstOrDefaultAsync(d =>
                d.MenuDiarioId == menuId &&
                d.ProductoId == productoId);

        if (detalle == null)
        {
            return NotFound(new
            {
                mensaje = "El producto no pertenece al menú."
            });
        }

        var inicioDia = menu.Fecha.Date;
        var finDia = inicioDia.AddDays(1);

        var tieneVentas = await _context.DetalleVentas
            .AnyAsync(d =>
                d.ProductoId == productoId &&
                d.Venta.FechaVenta >= inicioDia &&
                d.Venta.FechaVenta < finDia &&
                d.Venta.Estado == "REGISTRADA");

        if (tieneVentas)
        {
            return Conflict(new
            {
                mensaje = "El producto ya tiene ventas registradas este día. Márcalo como agotado en lugar de quitarlo."
            });
        }

        _context.MenuDetalles.Remove(detalle);

        menu.FechaActualizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto quitado del menú correctamente."
        });
    }
}