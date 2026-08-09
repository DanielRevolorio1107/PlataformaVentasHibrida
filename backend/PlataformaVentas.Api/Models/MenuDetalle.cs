namespace PlataformaVentas.Api.Models;

public class MenuDetalle
{
    public Guid Id { get; set; }

    public Guid MenuDiarioId { get; set; }

    public Guid ProductoId { get; set; }

    public bool Disponible { get; set; }

    public MenuDiario MenuDiario { get; set; } = null!;

    public Producto Producto { get; set; } = null!;
}