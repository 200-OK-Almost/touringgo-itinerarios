using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class GastoDto
    {
        public Guid Id { get; set; }
        public Guid ViajeId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public TipoMoneda Moneda { get; set; }
        public Guid PagadoPorUsuarioId { get; set; }
        public Guid RegistradoPorUsuarioId { get; set; }
        public DateTime FechaGasto { get; set; }
        public Guid? LiquidacionId { get; set; }
        public bool Liquidado { get; set; }
        public IReadOnlyCollection<DivisionGastoDto> Divisiones { get; set; } = new List<DivisionGastoDto>();
    }
}
