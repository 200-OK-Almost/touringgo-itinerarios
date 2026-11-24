using Itinerario.Application.DTOs.Gastos;
using Itinerario.Domain.Enums;

public class PrevisualizacionLiquidacionDto
{
    public Guid ViajeId { get; set; }
    public TipoMoneda Moneda { get; set; }
    public IReadOnlyCollection<Guid> GastoIds { get; set; }
        = new List<Guid>();
    public IReadOnlyCollection<TransferenciaLiquidacionDto> Transferencias { get; set; }
        = new List<TransferenciaLiquidacionDto>();
}