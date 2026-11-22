using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class LiquidacionDto
    {
        public Guid Id { get; set; }
        public Guid ViajeId { get; set; }
        public Guid RealizadaPorUsuarioId { get; set; }
        public TipoMoneda Moneda { get; set; }
        public DateTime FechaLiquidacion { get; set; }
        public IReadOnlyCollection<Guid> GastoIds { get; set; } = new List<Guid>();
        public IReadOnlyCollection<TransferenciaLiquidacionDto> Transferencias { get; set; } = new List<TransferenciaLiquidacionDto>();
    }
}
