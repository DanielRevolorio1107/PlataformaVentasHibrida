using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;
using PlataformaVentas.Api.DTOs.Sincronizacion;
using PlataformaVentas.Api.Models;

namespace PlataformaVentas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SincronizacionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public SincronizacionController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("recibir")]
    public async Task<IActionResult> Recibir(
        RecibirSincronizacionDto dto)
    {
        var claveConfigurada =
            _configuration["Sincronizacion:Clave"];

        if (string.IsNullOrWhiteSpace(claveConfigurada))
        {
            return StatusCode(500, new
            {
                mensaje =
                    "La clave de sincronización no está configurada."
            });
        }

        if (!Request.Headers.TryGetValue(
                "X-Sync-Key",
                out var claveRecibida) ||
            claveRecibida.ToString() != claveConfigurada)
        {
            return Unauthorized(new
            {
                mensaje =
                    "Clave de sincronización inválida."
            });
        }

        var entidad =
            dto.Entidad.Trim().ToUpperInvariant();

        var operacion =
            dto.TipoOperacion.Trim().ToUpperInvariant();

        return (entidad, operacion) switch
        {
            ("VENTA", "CREAR") =>
                await CrearVentaRemota(dto),

            ("VENTA", "ACTUALIZAR") =>
                await ActualizarVentaRemota(dto),

            ("PRODUCTO", "CREAR") =>
                await CrearProductoRemoto(dto),

            ("PRODUCTO", "ACTUALIZAR") =>
                await ActualizarProductoRemoto(dto),

            ("PRODUCTO", "ELIMINAR") =>
                await EliminarProductoRemoto(dto),

            ("USUARIO", "CREAR") =>
                await CrearUsuarioRemoto(dto),

            ("USUARIO", "ACTUALIZAR") =>
                await ActualizarUsuarioRemoto(dto),

            ("MENU", "CREAR") =>
                await CrearMenuRemoto(dto),

            ("MENU", "ACTUALIZAR") =>
                await ActualizarMenuRemoto(dto),

            _ => BadRequest(new
            {
                mensaje =
                    $"No se admite {dto.Entidad} - {dto.TipoOperacion}."
            })
        };
    }

    private async Task<IActionResult> CrearVentaRemota(
        RecibirSincronizacionDto dto)
    {
        VentaCrearPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<VentaCrearPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido de la venta no tiene un formato válido."
            });
        }

        if (datos == null)
        {
            return BadRequest(new
            {
                mensaje =
                    "No se pudieron interpretar los datos de la venta."
            });
        }

        if (datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "El identificador de la venta no coincide."
            });
        }

        var ventaExistente =
            await _context.Ventas
                .AnyAsync(v => v.Id == datos.Id);

        if (ventaExistente)
        {
            return Ok(new
            {
                mensaje =
                    "La venta ya había sido recibida.",
                ventaId = datos.Id,
                duplicada = true
            });
        }

        var usuarioExiste =
            await _context.Usuarios
                .AnyAsync(u =>
                    u.Id == datos.UsuarioId);

        if (!usuarioExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El usuario de la venta no existe en la base remota."
            });
        }

        var metodoPagoExiste =
            await _context.MetodosPago
                .AnyAsync(m =>
                    m.Id == datos.MetodoPagoId);

        if (!metodoPagoExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El método de pago no existe en la base remota."
            });
        }

        if (datos.Detalles == null ||
            datos.Detalles.Count == 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "La venta no contiene productos."
            });
        }

        var productoIds =
            datos.Detalles
                .Select(d => d.ProductoId)
                .Distinct()
                .ToList();

        var productosExistentes =
            await _context.Productos
                .Where(p =>
                    productoIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

        if (productosExistentes.Count !=
            productoIds.Count)
        {
            return Conflict(new
            {
                mensaje =
                    "Uno o más productos de la venta no existen en la base remota."
            });
        }

        var totalCalculado =
            datos.Detalles.Sum(d => d.Subtotal);

        if (totalCalculado != datos.Total)
        {
            return BadRequest(new
            {
                mensaje =
                    "El total recibido no coincide con los detalles de la venta."
            });
        }

        var venta = new Venta
        {
            Id = datos.Id,
            UsuarioId = datos.UsuarioId,
            MetodoPagoId = datos.MetodoPagoId,
            FechaVenta = datos.FechaVenta,
            Total = datos.Total,
            Estado = datos.Estado,
            Observaciones = datos.Observaciones,
            Sincronizado = true,
            FechaSincronizacion = DateTime.Now
        };

        foreach (var detalle in datos.Detalles)
        {
            venta.Detalles.Add(
                new DetalleVenta
                {
                    Id = detalle.Id,
                    VentaId = venta.Id,
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario =
                        detalle.PrecioUnitario,
                    Subtotal =
                        detalle.Subtotal
                });
        }

        _context.Ventas.Add(venta);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Venta recibida correctamente.",
            ventaId = venta.Id
        });
    }

    private async Task<IActionResult> ActualizarVentaRemota(
        RecibirSincronizacionDto dto)
    {
        VentaActualizarPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<VentaActualizarPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido de actualización no tiene un formato válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos de actualización no son válidos."
            });
        }

        var venta =
            await _context.Ventas
                .FirstOrDefaultAsync(v =>
                    v.Id == datos.Id);

        if (venta == null)
        {
            return Conflict(new
            {
                mensaje =
                    "La venta todavía no existe en la base remota."
            });
        }

        if (datos.Estado != "REGISTRADA" &&
            datos.Estado != "ANULADA")
        {
            return BadRequest(new
            {
                mensaje =
                    "El estado de la venta no es válido."
            });
        }

        venta.Estado = datos.Estado;
        venta.Sincronizado = true;
        venta.FechaSincronizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Venta actualizada correctamente.",
            venta.Id,
            venta.Estado
        });
    }

    private async Task<IActionResult> CrearProductoRemoto(
        RecibirSincronizacionDto dto)
    {
        ProductoCrearPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<ProductoCrearPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido del producto no tiene un formato válido."
            });
        }

        if (datos == null)
        {
            return BadRequest(new
            {
                mensaje =
                    "No se pudieron interpretar los datos del producto."
            });
        }

        if (datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "El identificador del producto no coincide."
            });
        }

        var productoExistente =
            await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.Id == datos.Id);

        if (productoExistente != null)
        {
            return Ok(new
            {
                mensaje =
                    "El producto ya había sido recibido.",
                productoId = datos.Id,
                duplicado = true
            });
        }

        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            return BadRequest(new
            {
                mensaje =
                    "El nombre del producto es obligatorio."
            });
        }

        if (datos.Precio < 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "El precio del producto no es válido."
            });
        }

        var producto = new Producto
        {
            Id = datos.Id,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            Precio = datos.Precio,
            Activo = datos.Activo,
            FechaCreacion = datos.FechaCreacion,
            FechaActualizacion =
                datos.FechaActualizacion
        };

        _context.Productos.Add(producto);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Producto recibido correctamente.",
            productoId = producto.Id
        });
    }

    private async Task<IActionResult> ActualizarProductoRemoto(
        RecibirSincronizacionDto dto)
    {
        ProductoActualizarPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<ProductoActualizarPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido de actualización del producto no es válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos del producto no son válidos."
            });
        }

        var producto =
            await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.Id == datos.Id);

        if (producto == null)
        {
            return Conflict(new
            {
                mensaje =
                    "El producto todavía no existe en la base remota."
            });
        }

        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            return BadRequest(new
            {
                mensaje =
                    "El nombre del producto es obligatorio."
            });
        }

        if (datos.Precio < 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "El precio del producto no es válido."
            });
        }

        producto.Nombre = datos.Nombre;
        producto.Descripcion = datos.Descripcion;
        producto.Precio = datos.Precio;
        producto.Activo = datos.Activo;
        producto.FechaActualizacion =
            datos.FechaActualizacion;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Producto actualizado correctamente.",
            producto.Id,
            producto.Nombre,
            producto.Activo
        });
    }

    private async Task<IActionResult> EliminarProductoRemoto(
        RecibirSincronizacionDto dto)
    {
        ProductoEliminarPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<ProductoEliminarPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido de eliminación del producto no es válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos del producto no son válidos."
            });
        }

        var producto =
            await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.Id == datos.Id);

        if (producto == null)
        {
            return Ok(new
            {
                mensaje =
                    "El producto ya no existe en la base remota.",
                productoId = datos.Id
            });
        }

        var tieneVentas =
            await _context.DetalleVentas
                .AnyAsync(d =>
                    d.ProductoId == datos.Id);

        if (tieneVentas)
        {
            return Conflict(new
            {
                mensaje =
                    "El producto no puede eliminarse de la base remota porque tiene ventas relacionadas."
            });
        }

        _context.Productos.Remove(producto);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Producto eliminado correctamente de la base remota.",
            productoId = datos.Id
        });
    }

    private async Task<IActionResult> CrearUsuarioRemoto(
        RecibirSincronizacionDto dto)
    {
        UsuarioCrearPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<UsuarioCrearPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido del usuario no tiene un formato válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos del usuario no son válidos."
            });
        }

        var usuarioExistente =
            await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Id == datos.Id);

        if (usuarioExistente != null)
        {
            return Ok(new
            {
                mensaje =
                    "El usuario ya había sido recibido.",
                usuarioId = datos.Id,
                duplicado = true
            });
        }

        var rolExiste =
            await _context.Roles
                .AnyAsync(r =>
                    r.Id == datos.RolId);

        if (!rolExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El rol del usuario no existe en la base remota."
            });
        }

        var nombreUsuarioOcupado =
            await _context.Usuarios
                .AnyAsync(u =>
                    u.NombreUsuario == datos.NombreUsuario);

        if (nombreUsuarioOcupado)
        {
            return Conflict(new
            {
                mensaje =
                    "Ya existe otro usuario con ese nombre de usuario en la base remota."
            });
        }

        if (string.IsNullOrWhiteSpace(datos.NombreCompleto) ||
            string.IsNullOrWhiteSpace(datos.NombreUsuario) ||
            string.IsNullOrWhiteSpace(datos.PasswordHash))
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos obligatorios del usuario están incompletos."
            });
        }

        var usuario = new Usuario
        {
            Id = datos.Id,
            RolId = datos.RolId,
            NombreCompleto = datos.NombreCompleto,
            NombreUsuario = datos.NombreUsuario,
            PasswordHash = datos.PasswordHash,
            Activo = datos.Activo,
            FechaCreacion = datos.FechaCreacion,
            FechaActualizacion =
                datos.FechaActualizacion
        };

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Usuario recibido correctamente.",
            usuarioId = usuario.Id
        });
    }

    private async Task<IActionResult> ActualizarUsuarioRemoto(
        RecibirSincronizacionDto dto)
    {
        UsuarioActualizarPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<UsuarioActualizarPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido de actualización del usuario no es válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos del usuario no son válidos."
            });
        }

        var usuario =
            await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Id == datos.Id);

        if (usuario == null)
        {
            return Conflict(new
            {
                mensaje =
                    "El usuario todavía no existe en la base remota."
            });
        }

        usuario.Activo = datos.Activo;
        usuario.FechaActualizacion =
            datos.FechaActualizacion;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Usuario actualizado correctamente.",
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Activo
        });
    }

    private async Task<IActionResult> CrearMenuRemoto(
        RecibirSincronizacionDto dto)
    {
        MenuCrearPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<MenuCrearPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido del menú no tiene un formato válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos del menú no son válidos."
            });
        }

        var menuExistente =
            await _context.MenusDiarios
                .AnyAsync(m =>
                    m.Id == datos.Id);

        if (menuExistente)
        {
            return Ok(new
            {
                mensaje =
                    "El menú ya había sido recibido.",
                menuId = datos.Id,
                duplicado = true
            });
        }

        var menuMismaFecha =
            await _context.MenusDiarios
                .AnyAsync(m =>
                    m.Fecha == datos.Fecha);

        if (menuMismaFecha)
        {
            return Conflict(new
            {
                mensaje =
                    "Ya existe otro menú para esa fecha en la base remota."
            });
        }

        var usuarioExiste =
            await _context.Usuarios
                .AnyAsync(u =>
                    u.Id == datos.UsuarioId);

        if (!usuarioExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El usuario del menú no existe en la base remota."
            });
        }

        if (datos.Detalles == null ||
            datos.Detalles.Count == 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "El menú debe contener al menos un producto."
            });
        }

        var productoIds =
            datos.Detalles
                .Select(d => d.ProductoId)
                .Distinct()
                .ToList();

        if (productoIds.Count !=
            datos.Detalles.Count)
        {
            return BadRequest(new
            {
                mensaje =
                    "El menú contiene productos duplicados."
            });
        }

        var productosExistentes =
            await _context.Productos
                .Where(p =>
                    productoIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

        if (productosExistentes.Count !=
            productoIds.Count)
        {
            return Conflict(new
            {
                mensaje =
                    "Uno o más productos del menú no existen en la base remota."
            });
        }

        var menu = new MenuDiario
        {
            Id = datos.Id,
            Fecha = datos.Fecha,
            UsuarioId = datos.UsuarioId,
            Activo = datos.Activo,
            FechaCreacion = datos.FechaCreacion,
            FechaActualizacion =
                datos.FechaActualizacion
        };

        foreach (var detalle in datos.Detalles)
        {
            menu.Detalles.Add(
                new MenuDetalle
                {
                    Id = detalle.Id,
                    MenuDiarioId = menu.Id,
                    ProductoId =
                        detalle.ProductoId,
                    Disponible =
                        detalle.Disponible
                });
        }

        _context.MenusDiarios.Add(menu);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Menú recibido correctamente.",
            menuId = menu.Id
        });
    }

    private async Task<IActionResult> ActualizarMenuRemoto(
        RecibirSincronizacionDto dto)
    {
        MenuActualizarPayload? datos;

        try
        {
            datos =
                JsonSerializer.Deserialize<MenuActualizarPayload>(
                    dto.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                mensaje =
                    "El contenido de actualización del menú no es válido."
            });
        }

        if (datos == null ||
            datos.Id != dto.EntidadId)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los datos del menú no son válidos."
            });
        }

        if (datos.Detalles == null)
        {
            return BadRequest(new
            {
                mensaje =
                    "Los detalles del menú no son válidos."
            });
        }

        var productoIds =
            datos.Detalles
                .Select(d => d.ProductoId)
                .Distinct()
                .ToList();

        if (productoIds.Count !=
            datos.Detalles.Count)
        {
            return BadRequest(new
            {
                mensaje =
                    "El menú contiene productos duplicados."
            });
        }

        var productosExistentes =
            await _context.Productos
                .Where(p =>
                    productoIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

        if (productosExistentes.Count !=
            productoIds.Count)
        {
            return Conflict(new
            {
                mensaje =
                    "Uno o más productos del menú no existen en la base remota."
            });
        }

        var menu =
            await _context.MenusDiarios
                .Include(m => m.Detalles)
                .FirstOrDefaultAsync(m =>
                    m.Id == datos.Id);

        if (menu == null)
        {
            var usuarioExiste =
                await _context.Usuarios
                    .AnyAsync(u =>
                        u.Id == datos.UsuarioId);

            if (!usuarioExiste)
            {
                return Conflict(new
                {
                    mensaje =
                        "El usuario del menú no existe en la base remota."
                });
            }

            var menuMismaFecha =
                await _context.MenusDiarios
                    .AnyAsync(m =>
                        m.Fecha == datos.Fecha);

            if (menuMismaFecha)
            {
                return Conflict(new
                {
                    mensaje =
                        "Ya existe otro menú para esa fecha en la base remota."
                });
            }

            menu = new MenuDiario
            {
                Id = datos.Id,
                Fecha = datos.Fecha,
                UsuarioId = datos.UsuarioId,
                Activo = datos.Activo,
                FechaCreacion = datos.FechaCreacion,
                FechaActualizacion =
                    datos.FechaActualizacion
            };

            foreach (var detalle in datos.Detalles)
            {
                menu.Detalles.Add(
                    new MenuDetalle
                    {
                        Id = detalle.Id,
                        MenuDiarioId = menu.Id,
                        ProductoId =
                            detalle.ProductoId,
                        Disponible =
                            detalle.Disponible
                    });
            }

            _context.MenusDiarios.Add(menu);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    "El menú no existía y fue creado durante la sincronización.",
                menu.Id,
                creado = true,
                cantidadProductos =
                    menu.Detalles.Count
            });
        }

        menu.Activo = datos.Activo;
        menu.FechaActualizacion =
            datos.FechaActualizacion;

        var idsRecibidos =
            datos.Detalles
                .Select(d => d.Id)
                .ToHashSet();

        var detallesAEliminar =
            menu.Detalles
                .Where(d =>
                    !idsRecibidos.Contains(d.Id))
                .ToList();

        _context.MenuDetalles.RemoveRange(
            detallesAEliminar
        );

        foreach (var detalleDto in datos.Detalles)
        {
            var detalleExistente =
                menu.Detalles
                    .FirstOrDefault(d =>
                        d.Id == detalleDto.Id);

            if (detalleExistente != null)
            {
                detalleExistente.ProductoId =
                    detalleDto.ProductoId;

                detalleExistente.Disponible =
                    detalleDto.Disponible;
            }
            else
            {
                menu.Detalles.Add(
                    new MenuDetalle
                    {
                        Id = detalleDto.Id,
                        MenuDiarioId = menu.Id,
                        ProductoId =
                            detalleDto.ProductoId,
                        Disponible =
                            detalleDto.Disponible
                    });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje =
                "Menú actualizado correctamente.",
            menu.Id,
            creado = false,
            cantidadProductos =
                datos.Detalles.Count
        });
    }

    private class VentaCrearPayload
    {
        public Guid Id { get; set; }

        public Guid UsuarioId { get; set; }

        public Guid MetodoPagoId { get; set; }

        public DateTime FechaVenta { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; }
            = string.Empty;

        public string? Observaciones { get; set; }

        public List<VentaDetallePayload> Detalles
        {
            get;
            set;
        } = [];
    }

    private class VentaDetallePayload
    {
        public Guid Id { get; set; }

        public Guid ProductoId { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }
    }

    private class VentaActualizarPayload
    {
        public Guid Id { get; set; }

        public string Estado { get; set; }
            = string.Empty;
    }

    private class ProductoCrearPayload
    {
        public Guid Id { get; set; }

        public string Nombre { get; set; }
            = string.Empty;

        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }

    private class ProductoActualizarPayload
    {
        public Guid Id { get; set; }

        public string Nombre { get; set; }
            = string.Empty;

        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public bool Activo { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }

    private class ProductoEliminarPayload
    {
        public Guid Id { get; set; }
    }

    private class UsuarioCrearPayload
    {
        public Guid Id { get; set; }

        public Guid RolId { get; set; }

        public string NombreCompleto { get; set; }
            = string.Empty;

        public string NombreUsuario { get; set; }
            = string.Empty;

        public string PasswordHash { get; set; }
            = string.Empty;

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }

    private class UsuarioActualizarPayload
    {
        public Guid Id { get; set; }

        public bool Activo { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }

    private class MenuCrearPayload
    {
        public Guid Id { get; set; }

        public DateTime Fecha { get; set; }

        public Guid UsuarioId { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public List<MenuDetallePayload> Detalles { get; set; }
            = [];
    }

    private class MenuActualizarPayload
    {
        public Guid Id { get; set; }

        public DateTime Fecha { get; set; }

        public Guid UsuarioId { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public List<MenuDetallePayload> Detalles { get; set; }
            = [];
    }

    private class MenuDetallePayload
    {
        public Guid Id { get; set; }

        public Guid ProductoId { get; set; }

        public bool Disponible { get; set; }
    }
}