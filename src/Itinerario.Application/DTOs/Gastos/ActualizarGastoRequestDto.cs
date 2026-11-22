using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class ActualizarGastoRequestDto
    {
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto {  get; set; }
        public TipoMoneda Moneda { get; set; }
        public Guid PagadoPorMiembroId { get; set; }
        public DateTime FechaGasto { get; set; }
        public IReadOnlyCollection<Guid> ParticipantesIds { get; set; } = new List<Guid>();
    }
}
