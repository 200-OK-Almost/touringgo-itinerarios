using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class BalanceGastosDto
    {
        public Guid ViajeId { get; set; }
        public TipoMoneda Moneda { get; set; }
        public IReadOnlyCollection<BalanceParticipanteDto> Balances { get; set; } = new List<BalanceParticipanteDto>();
    }
}
