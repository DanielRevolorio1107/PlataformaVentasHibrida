using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PlataformaVentas.Api.Data;

namespace PlataformaVentas.Api.Services;

public class SincronizacionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SincronizacionService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SincronizacionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SincronizacionService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var intervaloSegundos =
            _configuration.GetValue<int>(
                "Sincronizacion:IntervaloSegundos");

        if (intervaloSegundos <= 0)
        {
            intervaloSegundos = 30;
        }

        var urlRemota =
            _configuration["Sincronizacion:UrlRemota"];

        _logger.LogInformation(
            "Servicio de sincronización iniciado.");

        if (string.IsNullOrWhiteSpace(urlRemota))
        {
            _logger.LogInformation(
                "La URL remota todavía no está configurada. Las ventas permanecerán pendientes.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarColaAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ocurrió un error general en el servicio de sincronización.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(intervaloSegundos),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcesarColaAsync(
        CancellationToken cancellationToken)
    {
        var urlRemota =
            _configuration["Sincronizacion:UrlRemota"];

        var claveSincronizacion =
            _configuration["Sincronizacion:Clave"];

        if (string.IsNullOrWhiteSpace(urlRemota))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(claveSincronizacion))
        {
            _logger.LogWarning(
                "La clave de sincronización no está configurada.");

            return;
        }

        var maximoIntentos =
            _configuration.GetValue<int>(
                "Sincronizacion:MaximoIntentos");

        if (maximoIntentos <= 0)
        {
            maximoIntentos = 5;
        }

        using var scope =
            _scopeFactory.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var limiteRecuperacion =
            DateTime.Now.AddMinutes(-2);

        var atascados =
            await context.ColaSincronizacion
                .Where(c =>
                    c.Estado == "PROCESANDO" &&
                    c.FechaUltimoIntento != null &&
                    c.FechaUltimoIntento < limiteRecuperacion)
                .ToListAsync(cancellationToken);

        if (atascados.Count > 0)
        {
            foreach (var item in atascados)
            {
                item.Estado = "PENDIENTE";
            }

            await context.SaveChangesAsync(
                cancellationToken);
        }

        var pendientes =
            await context.ColaSincronizacion
                .Where(c =>
                    c.Estado == "PENDIENTE" &&
                    c.Intentos < maximoIntentos)
                .OrderBy(c => c.FechaCreacion)
                .Take(20)
                .ToListAsync(cancellationToken);

        if (pendientes.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Se encontraron {Cantidad} registro(s) pendientes.",
            pendientes.Count);

        var cliente =
            _httpClientFactory.CreateClient();

        cliente.DefaultRequestHeaders.Add(
            "X-Sync-Key",
            claveSincronizacion);

        foreach (var pendiente in pendientes)
        {
            try
            {
                pendiente.Estado = "PROCESANDO";
                pendiente.Intentos++;
                pendiente.FechaUltimoIntento = DateTime.Now;

                await context.SaveChangesAsync(
                    cancellationToken);

                var datosEnvio = new
                {
                    pendiente.Id,
                    pendiente.Entidad,
                    pendiente.EntidadId,
                    pendiente.TipoOperacion,
                    pendiente.Payload
                };

                var url =
                    $"{urlRemota.TrimEnd('/')}/api/sincronizacion/recibir";

                var respuesta =
                    await cliente.PostAsJsonAsync(
                        url,
                        datosEnvio,
                        cancellationToken);

                if (respuesta.IsSuccessStatusCode)
                {
                    pendiente.Estado = "SINCRONIZADO";
                    pendiente.FechaSincronizacion =
                        DateTime.Now;

                    pendiente.UltimoError = null;

                    await context.SaveChangesAsync(
                        cancellationToken);

                    await ActualizarEstadoVentaAsync(
                        context,
                        pendiente.Entidad,
                        pendiente.EntidadId,
                        cancellationToken);

                    _logger.LogInformation(
                        "Sincronización correcta: {Entidad} {EntidadId} - {Operacion}",
                        pendiente.Entidad,
                        pendiente.EntidadId,
                        pendiente.TipoOperacion);
                }
                else
                {
                    var contenido =
                        await respuesta.Content
                            .ReadAsStringAsync(
                                cancellationToken);

                    RegistrarError(
                        pendiente,
                        maximoIntentos,
                        $"HTTP {(int)respuesta.StatusCode}: {contenido}");

                    await context.SaveChangesAsync(
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                pendiente.Estado = "PENDIENTE";

                pendiente.Intentos =
                    Math.Max(0, pendiente.Intentos - 1);

                pendiente.UltimoError =
                    ex.Message.Length > 1000
                        ? ex.Message[..1000]
                        : ex.Message;

                await context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogWarning(
                    "La API remota no está disponible. " +
                    "La operación {Entidad} {EntidadId} permanecerá pendiente.",
                    pendiente.Entidad,
                    pendiente.EntidadId);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                pendiente.Estado = "PENDIENTE";

                pendiente.Intentos =
                    Math.Max(0, pendiente.Intentos - 1);

                pendiente.UltimoError =
                    "Tiempo de espera agotado al intentar conectar con la API remota.";

                await context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (Exception ex)
            {
                RegistrarError(
                    pendiente,
                    maximoIntentos,
                    ex.Message);

                await context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogWarning(
                    ex,
                    "No se pudo sincronizar {Entidad} {EntidadId}. Intento {Intento}.",
                    pendiente.Entidad,
                    pendiente.EntidadId,
                    pendiente.Intentos);
            }
        }
    }

    private static void RegistrarError(
        Models.ColaSincronizacion pendiente,
        int maximoIntentos,
        string error)
    {
        pendiente.UltimoError =
            error.Length > 1000
                ? error[..1000]
                : error;

        pendiente.Estado =
            pendiente.Intentos >= maximoIntentos
                ? "ERROR"
                : "PENDIENTE";
    }

    private static async Task ActualizarEstadoVentaAsync(
        AppDbContext context,
        string entidad,
        Guid entidadId,
        CancellationToken cancellationToken)
    {
        if (entidad != "Venta")
        {
            return;
        }

        var existenCambiosPendientes =
            await context.ColaSincronizacion
                .AnyAsync(c =>
                    c.Entidad == "Venta" &&
                    c.EntidadId == entidadId &&
                    c.Estado != "SINCRONIZADO",
                    cancellationToken);

        var venta =
            await context.Ventas
                .FirstOrDefaultAsync(
                    v => v.Id == entidadId,
                    cancellationToken);

        if (venta == null)
        {
            return;
        }

        venta.Sincronizado =
            !existenCambiosPendientes;

        venta.FechaSincronizacion =
            venta.Sincronizado
                ? DateTime.Now
                : null;

        await context.SaveChangesAsync(
            cancellationToken);
    }
}