using Itinerario.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Itinerario.Domain.Entities
{
    public class Liquidacion
    {
        public Guid Id { get; set; }
        public Guid ViajeId { get; set; }
        public TipoMoneda Moneda { get; set; }
        public Guid RealizadaPorUsuarioId { get; set; }
        public DateTimeOffset FechaLiquidacion { get; set; }

        public ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
        public Viaje Viaje { get; set; } = null!;
        public ICollection<TransferenciaLiquidacion> Transferencias { get; set; } = new List<TransferenciaLiquidacion>();
    }
}
