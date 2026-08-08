using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;
using PlataformaVentas.Api.DTOs.Productos;
using PlataformaVentas.Api.Models;
using Microsoft.AspNetCore.Authorization;

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
    public async Task<IActionResult> EditarProducto(Guid id, EditarProductoDto dto)
    {
        var producto = await _context.Productos.FindAsync(id);

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

        await _context.SaveChangesAsync();

        return Ok(producto);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> DesactivarProducto(Guid id)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        producto.Activo = false;
        producto.FechaActualizacion = DateTime.Now;

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
        var producto = await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        var tieneVentas = await _context.DetalleVentas
            .AnyAsync(d => d.ProductoId == id);

        if (tieneVentas)
        {
            return Conflict(new
            {
                mensaje = "El producto no puede eliminarse porque ya está relacionado con ventas. Puede desactivarlo."
            });
        }

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
        var producto = await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                mensaje = "Producto no encontrado"
            });
        }

        return Ok(producto);
    }
}