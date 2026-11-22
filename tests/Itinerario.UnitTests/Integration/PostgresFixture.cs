using Itinerario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Itinerario.UnitTests.Integration;

public sealed class PostgresFixture
{
    private readonly PostgreSqlContainer _container;
    private readonly Lazy<Task> _inicializacion;

    private PostgresFixture()
    {
        _container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("itinerarios_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _inicializacion = new Lazy<Task>(InicializarCoreAsync);
    }

    public static PostgresFixture Instance { get; } = new();

    public async Task<ApplicationDbContext> CreateContextAsync()
    {
        await _inicializacion.Value;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private async Task InicializarCoreAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }
}
