using Itinerario.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Itinerario.Domain.Entities
{
    public class TransferenciaLiquidacion
    {
        public Guid Id { get; set; }
        public Guid ViajeId { get; set; }
        public Guid LiquidacionId { get; set; }
        public Guid DeParticipanteId { get; set; }
        public Guid AParticipanteId { get; set; }
        public decimal Monto { get; set; }

        public MiembroViaje DeParticipante { get; set; } = null!;
        public MiembroViaje AParticipante { get; set; } = null!;
        public Liquidacion Liquidacion { get; set; } = null!;
    }
}
