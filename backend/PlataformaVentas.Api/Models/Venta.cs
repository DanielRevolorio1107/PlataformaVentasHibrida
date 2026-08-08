namespace PlataformaVentas.Api.Models;

public class Venta
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid MetodoPagoId { get; set; }

    public DateTime FechaVenta { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = "REGISTRADA";

    public string? Observaciones { get; set; }

    public bool Sincronizado { get; set; }

    public DateTime? FechaSincronizacion { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public MetodoPago MetodoPago { get; set; } = null!;

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}