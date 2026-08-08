namespace PlataformaVentas.Api.Models;

public class MetodoPago
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}