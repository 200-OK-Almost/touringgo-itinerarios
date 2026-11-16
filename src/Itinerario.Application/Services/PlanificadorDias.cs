namespace Itinerario.Application.Services;

public static class PlanificadorDias
{
    public static IReadOnlyList<DiaPlanificado> Calcular(
        DateOnly? fechaInicio,
        DateOnly? fechaFin)
    {
        if (fechaInicio is null || fechaFin is null || fechaFin < fechaInicio)
        {
            return new[] { new DiaPlanificado(1, fechaInicio) };
        }

        var dias = new List<DiaPlanificado>();

        for (var fecha = fechaInicio.Value; fecha <= fechaFin.Value; fecha = fecha.AddDays(1))
        {
            dias.Add(new DiaPlanificado((short)(dias.Count + 1), fecha));
        }

        return dias;
    }
}

public sealed record DiaPlanificado(short NumeroDia, DateOnly? Fecha);
