using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;
using PlataformaVentas.Api.DTOs.Productos;
using PlataformaVentas.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace PlataformaVentas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductosController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> CrearProducto(CrearProductoDto dto)
    {
        var producto = new Producto
        {
            Id = Guid.NewGuid(),
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Precio = dto.Precio,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        _context.Productos.Add(producto);

        var payload = JsonSerializer.Serialize(new
        {
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Activo,
            producto.FechaCreacion,
            producto.FechaActualizacion
        });

        var pendienteSincronizacion = new ColaSincronizacion
        {
            Id = Guid.NewGuid(),
            Entidad = "Producto",
            EntidadId = producto.Id,
            TipoOperacion = "CREAR",
            Payload = payload,
            Estado = "PENDIENTE",
            Intentos = 0,
            FechaCreacion = DateTime.Now
        };

        _context.ColaSincronizacion.Add(
            pendienteSincronizacion
        );

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenerProducto),
            new { id = producto.Id },
            producto
        );
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerProductos()
    {
        var productos = await _context.Productos
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return Ok(productos);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditarProducto(
        Guid id,
        EditarProductoDto dto)
    {
        var producto =
            await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = dto.Descripcion?.Trim();
        producto.Precio = dto.Precio;
        producto.Activo = dto.Activo;
        producto.FechaActualizacion = DateTime.Now;

        var pendiente =
            CrearActualizacionProducto(producto);

        _context.ColaSincronizacion.Add(pendiente);

        await _context.SaveChangesAsync();

        return Ok(producto);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> DesactivarProducto(Guid id)
    {
        var producto =
            await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        producto.Activo = false;
        producto.FechaActualizacion = DateTime.Now;

        var pendiente =
            CrearActualizacionProducto(producto);

        _context.ColaSincronizacion.Add(pendiente);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto desactivado correctamente",
            producto.Id,
            producto.Nombre,
            producto.Activo
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> EliminarProducto(Guid id)
    {
        var producto =
            await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        var tieneVentas =
            await _context.DetalleVentas
                .AnyAsync(d => d.ProductoId == id);

        if (tieneVentas)
        {
            return Conflict(new
            {
                mensaje =
                    "El producto no puede eliminarse porque ya está relacionado con ventas. Puede desactivarlo."
            });
        }

        var estaEnMenu =
            await _context.MenuDetalles
                .AnyAsync(m => m.ProductoId == id);

        if (estaEnMenu)
        {
            return Conflict(new
            {
                mensaje =
                    "El producto no puede eliminarse porque está relacionado con un menú diario. Puede desactivarlo."
            });
        }

        var payload = JsonSerializer.Serialize(new
        {
            Id = producto.Id
        });

        var pendiente = new ColaSincronizacion
        {
            Id = Guid.NewGuid(),
            Entidad = "Producto",
            EntidadId = producto.Id,
            TipoOperacion = "ELIMINAR",
            Payload = payload,
            Estado = "PENDIENTE",
            Intentos = 0,
            FechaCreacion = DateTime.Now
        };

        _context.ColaSincronizacion.Add(pendiente);

        _context.Productos.Remove(producto);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto eliminado correctamente"
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerProducto(Guid id)
    {
        var producto =
            await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        return Ok(producto);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/activar")]
    public async Task<IActionResult> ActivarProducto(Guid id)
    {
        var producto =
            await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        producto.Activo = true;
        producto.FechaActualizacion = DateTime.Now;

        var pendiente =
            CrearActualizacionProducto(producto);

        _context.ColaSincronizacion.Add(pendiente);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto activado correctamente",
            producto.Id,
            producto.Nombre,
            producto.Activo
        });
    }

    private ColaSincronizacion CrearActualizacionProducto(
        Producto producto)
    {
        var payload = JsonSerializer.Serialize(new
        {
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Activo,
            producto.FechaActualizacion
        });

        return new ColaSincronizacion
        {
            Id = Guid.NewGuid(),
            Entidad = "Producto",
            EntidadId = producto.Id,
            TipoOperacion = "ACTUALIZAR",
            Payload = payload,
            Estado = "PENDIENTE",
            Intentos = 0,
            FechaCreacion = DateTime.Now
        };
    }
}