using Arquivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Arquivos.API;

public sealed class ArquivosDbContextFactory : IDesignTimeDbContextFactory<ArquivosDbContext>
{
    public ArquivosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ArquivosDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5433;Database=base;Username=postgres;Password=postgres;Search Path=arquivos",
                npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "arquivos"))
            .Options;
        return new ArquivosDbContext(options);
    }
}
