namespace PlataformaVentas.Api.Models;

public class DetalleVenta
{
    public Guid Id { get; set; }

    public Guid VentaId { get; set; }

    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public Venta Venta { get; set; } = null!;

    public Producto Producto { get; set; } = null!;
}