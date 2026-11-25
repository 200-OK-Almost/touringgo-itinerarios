using Itinerario.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Itinerario.Domain.Entities
{
    public class DivisionGasto
    {
        public Guid Id { get; set; }
        public Guid ViajeId { get; set; }
        public Guid GastoId { get; set; }
        
        public Guid ParticipanteId { get; set; }
        public decimal MontoAsignado { get; set; }

        public Gasto Gasto { get; set; } = null!;
        public MiembroViaje Participante { get; set; } = null!;
    }
}
