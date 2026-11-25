using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class TransferenciaLiquidacionDto
    {
        public Guid DeParticipanteId { get; set; }
        public Guid AParticipanteId { get; set; }
        public decimal Monto { get; set; }
    }
}
