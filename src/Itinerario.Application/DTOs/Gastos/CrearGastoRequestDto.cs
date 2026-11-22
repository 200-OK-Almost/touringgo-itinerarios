using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class CrearGastoRequestDto
    {
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public TipoMoneda Moneda { get; set; } 
        public Guid PagadoPorUsuarioId { get; set; }
        public DateTime FechaGasto { get; set; }
        public IReadOnlyCollection<Guid> ParticipantesIds { get; set; } = new List<Guid>();
    }
}
