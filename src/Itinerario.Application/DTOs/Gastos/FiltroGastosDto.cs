using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs.Gastos
{
    public class FiltroGastosDto
    {
        public EstadoGastoFiltro Estado { get; set; } = EstadoGastoFiltro.Todos;
        public Guid? UsuarioId { get; set; } = null;
        public TipoMoneda? Moneda { get; set; } = null;
        public DateTime? FechaDesde { get; set; } = null;
        public DateTime? FechaHasta { get; set; } = null;
        public int Pagina { get; set; } = 1;
        public int Tamano { get; set; } = 20;
    }
}
