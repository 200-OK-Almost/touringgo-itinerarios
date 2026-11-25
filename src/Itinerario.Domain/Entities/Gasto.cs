using Itinerario.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Itinerario.Domain.Entities
{
    public class Gasto
    {
        public Guid Id { get; set; }
        public Guid ViajeId { get; set; }
        
        public Guid PagadoPorMiembroId { get; set; }
        public Guid RegistradoPorMiembroId { get; set; }
        public Guid? LiquidacionId { get; set; }

        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public TipoMoneda Moneda { get; set; }
        
        public DateTimeOffset FechaGasto { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public DateTimeOffset FechaActualizacion { get; set; }
        
        [Timestamp]
        public uint Version { get; set; }

        public Viaje Viaje { get; set; } = null!;
        public MiembroViaje PagadoPorMiembro { get; set; } = null!;
        public Liquidacion Liquidacion { get; set; } = null!;
        public ICollection<DivisionGasto> Divisiones { get; set; } = new List<DivisionGasto>();
    }
}
