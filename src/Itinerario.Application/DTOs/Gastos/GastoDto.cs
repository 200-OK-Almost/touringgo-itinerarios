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
        public Guid PagadoPorMiembroId { get; set; }
        public Guid RegistradoPorMiembroId { get; set; }
        public DateTimeOffset FechaGasto { get; set; }
        public Guid? LiquidacionId { get; set; }
        public bool Liquidado { get; set; }
        public IEnumerable<DivisionGastoDto> Divisiones { get; set; } = new List<DivisionGastoDto>();
    }
}
