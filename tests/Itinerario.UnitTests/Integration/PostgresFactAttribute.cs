namespace Itinerario.UnitTests.Integration;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS") != "1")
        {
            Skip = "Definir RUN_POSTGRES_INTEGRATION_TESTS=1 y tener Docker para ejecutar.";
        }
    }
}
