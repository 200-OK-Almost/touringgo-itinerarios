using Microsoft.EntityFrameworkCore;

namespace Itinerario.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }
    // Registrar entidades

}
