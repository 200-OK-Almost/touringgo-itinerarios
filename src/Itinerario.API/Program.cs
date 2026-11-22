using System.Text.Json.Serialization;
using Itinerario.API.Extensions;
using Itinerario.API.Filters;
using Itinerario.API.RateLimiting;
using Itinerario.API.Services;
using Itinerario.Application;
using Itinerario.Application.Configuration;
using Itinerario.Application.Interfaces;
using Itinerario.Infrastructure;
using Itinerario.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services
    .AddOptions<AsistenteOptions>()
    .Bind(builder.Configuration.GetSection(AsistenteOptions.SectionName))
    .Validate(o => o.TopK > 0, "Asistente:TopK debe ser mayor que cero.")
    .Validate(o => o.MinResultados > 0, "Asistente:MinResultados debe ser mayor que cero.")
    .Validate(
        o => o.MinResultados <= o.TopK,
        "Asistente:MinResultados no puede superar a Asistente:TopK.")
    .Validate(
        o => !o.SimilitudMinima.HasValue
            || (o.SimilitudMinima.Value >= 0 && o.SimilitudMinima.Value <= 1),
        "Asistente:SimilitudMinima debe estar entre 0 y 1.")
    .Validate(o => o.RadioMetrosDefault > 0, "Asistente:RadioMetrosDefault debe ser mayor que cero.")
    .Validate(o => o.MaxMensajeCaracteres > 0, "Asistente:MaxMensajeCaracteres debe ser mayor que cero.")
    .Validate(o => o.MaxHistorial >= 0, "Asistente:MaxHistorial no puede ser negativo.")
    .Validate(o => o.IntentTimeoutSeconds > 0, "Asistente:IntentTimeoutSeconds debe ser mayor que cero.")
    .Validate(o => o.GenerationTimeoutSeconds > 0, "Asistente:GenerationTimeoutSeconds debe ser mayor que cero.")
    .Validate(
        o => o.RateLimit.PermitLimitPorUsuario > 0,
        "Asistente:RateLimit:PermitLimitPorUsuario debe ser mayor que cero.")
    .Validate(
        o => o.RateLimit.PermitLimitPorViaje > 0,
        "Asistente:RateLimit:PermitLimitPorViaje debe ser mayor que cero.")
    .Validate(
        o => o.RateLimit.WindowSeconds > 0,
        "Asistente:RateLimit:WindowSeconds debe ser mayor que cero.")
    .Validate(
        o => o.RateLimit.QueueLimit >= 0,
        "Asistente:RateLimit:QueueLimit no puede ser negativo.")
    .ValidateOnStart();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Leer el JWT desde la cookie "access_token" si no viene en el header Authorization.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out var token) &&
                    !string.IsNullOrWhiteSpace(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAsistenteRateLimitStore, RedisAsistenteRateLimitStore>();

var app = builder.Build();

app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

// Aplicar migraciones al arrancar (omitido en entorno de testing para permitir DbContext en memoria).
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "No se pudo preparar la base de datos al arrancar (¿Postgres aún no iniciado?).");
    }
}

app.Run();