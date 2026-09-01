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


        if (!string.Equals(
                dto.Entidad,
                "Venta",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                mensaje =
                    $"La entidad {dto.Entidad} todavía no puede sincronizarse."
            });
        }

        return dto.TipoOperacion.ToUpperInvariant() switch
        {
            "CREAR" =>
                await CrearVentaRemota(dto),

            "ACTUALIZAR" =>
                await ActualizarVentaRemota(dto),

            _ => BadRequest(new
            {
                mensaje =
                    $"La operación {dto.TipoOperacion} no es válida."
            })
        };
    }

    private async Task<IActionResult> CrearVentaRemota(
        RecibirSincronizacionDto dto)
    {
        VentaCrearPayload? datos;

        try
        {
            datos = JsonSerializer.Deserialize<VentaCrearPayload>(
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
            // Esto permite que un reintento sea seguro.
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

            // En la base remota ya está sincronizada.
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
}