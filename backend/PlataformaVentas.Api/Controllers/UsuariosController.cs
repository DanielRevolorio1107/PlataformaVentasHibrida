using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;
using PlataformaVentas.Api.DTOs.Usuarios;
using PlataformaVentas.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace PlataformaVentas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(
     AppDbContext context,
     IPasswordHasher<Usuario> passwordHasher,
     IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> CrearUsuario(CrearUsuarioDto dto)
    {
        var nombreUsuario = dto.NombreUsuario.Trim();

        var usuarioExiste = await _context.Usuarios
            .AnyAsync(u => u.NombreUsuario == nombreUsuario);

        if (usuarioExiste)
        {
            return Conflict(new
            {
                mensaje = "El nombre de usuario ya está registrado"
            });
        }

        var rolExiste = await _context.Roles
            .AnyAsync(r => r.Id == dto.RolId && r.Activo);

        if (!rolExiste)
        {
            return BadRequest(new
            {
                mensaje = "El rol indicado no existe o está inactivo"
            });
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            RolId = dto.RolId,
            NombreCompleto = dto.NombreCompleto.Trim(),
            NombreUsuario = nombreUsuario,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        usuario.PasswordHash =
            _passwordHasher.HashPassword(usuario, dto.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            usuario.Id,
            usuario.NombreCompleto,
            usuario.NombreUsuario,
            usuario.RolId,
            usuario.Activo,
            usuario.FechaCreacion
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var nombreUsuario = dto.NombreUsuario.Trim();

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u =>
                u.NombreUsuario == nombreUsuario &&
                u.Activo);

        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensaje = "Usuario o contraseña incorrectos"
            });
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            dto.Password
        );

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                mensaje = "Usuario o contraseña incorrectos"
            });
        }
        var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
    new Claim(ClaimTypes.Name, usuario.NombreUsuario),
    new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
};

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiracion = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:ExpirationMinutes"));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiracion,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            mensaje = "Inicio de sesión correcto",
            token = tokenString,
            expiracion,
            usuario = new
            {
                usuario.Id,
                usuario.NombreCompleto,
                usuario.NombreUsuario,
                rol = usuario.Rol.Nombre
            }
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.Rol)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new
            {
                u.Id,
                u.NombreCompleto,
                u.NombreUsuario,
                Rol = u.Rol.Nombre,
                u.Activo,
                u.FechaCreacion
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> DesactivarUsuario(Guid id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "Usuario no encontrado"
            });
        }

        var usuarioActualIdTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(usuarioActualIdTexto, out Guid usuarioActualId)
            && usuarioActualId == id)
        {
            return BadRequest(new
            {
                mensaje = "No puede desactivar su propio usuario."
            });
        }

        usuario.Activo = false;
        usuario.FechaActualizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario desactivado correctamente",
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Activo
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/activar")]
    public async Task<IActionResult> ActivarUsuario(Guid id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "Usuario no encontrado"
            });
        }

        usuario.Activo = true;
        usuario.FechaActualizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario activado correctamente",
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Activo
        });
    }
}