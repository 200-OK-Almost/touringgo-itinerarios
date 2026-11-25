using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class LiquidarGastosRequestDto
    {
        public IReadOnlyCollection<Guid> GastoIds { get; set; } = new List<Guid>();
    }
}
