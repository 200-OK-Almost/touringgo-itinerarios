using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class BalanceParticipanteDto
    {
        public Guid UsuarioId { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal TotalAsignado { get; set; }
        public decimal BalanceNeto { get; set; }
    }
}
