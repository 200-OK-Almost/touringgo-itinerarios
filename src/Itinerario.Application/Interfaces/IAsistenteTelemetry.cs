namespace Itinerario.Application.Interfaces;

public interface IAsistenteTelemetry
{
    void RegistrarEtapa(string etapa, TimeSpan duracion, Guid? viajeId = null);

    void RegistrarResultado(string resultado, int cantidadRecomendaciones, Guid? viajeId = null);

    void RegistrarDetalle(string detalle, Guid? viajeId = null);
}
