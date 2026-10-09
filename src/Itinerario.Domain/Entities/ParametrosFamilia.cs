namespace Itinerario.Domain.Entities;

public class ParametrosFamilia
{
    public Guid ViajeId { get; set; }
    public short CantidadAcompanantes { get; set; }
    public bool HayNinos { get; set; }
    public short CantidadNinos { get; set; }
    public bool MovilidadReducida { get; set; }
    public bool RequiereRitmoBajo { get; set; }
    public bool RequiereMenuInfantil { get; set; }

    public Viaje? Viaje { get; set; }
}
