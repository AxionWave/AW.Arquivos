using Arquivos.Application.Abstractions;
using Arquivos.Application.Arquivos;
using Arquivos.Infrastructure.Auth;
using Arquivos.Infrastructure.Persistence;
using Arquivos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arquivos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section));
        services.AddSingleton<IArquivoStorage, LocalFileStorage>();
        services.AddScoped<IArquivoRepository, ArquivoRepository>();
        services.AddScoped<IArquivoService, ArquivoService>();

        var conn = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5433;Database=base;Username=postgres;Password=postgres;Search Path=arquivos";

        services.AddDbContext<ArquivosDbContext>(o =>
            o.UseNpgsql(conn, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "arquivos")));

        return services;
    }
}
