using System.Text.Json;
using Arquivos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Arquivos.Infrastructure.Persistence;

public sealed class ArquivosDbContext(DbContextOptions<ArquivosDbContext> options) : DbContext(options)
{
    public DbSet<Arquivo> Arquivos => Set<Arquivo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("arquivos");

        var jsonOptions = new JsonSerializerOptions();
        var metadadosConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions)
                 ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        modelBuilder.Entity<Arquivo>(e =>
        {
            e.ToTable("arquivos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Token).IsRequired().HasMaxLength(80);
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.NomeOriginal).IsRequired().HasMaxLength(512);
            e.Property(x => x.NomeArmazenado).IsRequired().HasMaxLength(256);
            e.Property(x => x.Extensao).HasMaxLength(32);
            e.Property(x => x.ContentType).IsRequired().HasMaxLength(255);
            e.Property(x => x.TamanhoBytes).IsRequired();
            e.Property(x => x.ChecksumSha256).IsRequired().HasMaxLength(64);
            e.Property(x => x.CaminhoStorage).IsRequired().HasMaxLength(1024);
            e.Property(x => x.ProvedorStorage).IsRequired().HasMaxLength(32);
            e.Property(x => x.SistemaOrigem).HasMaxLength(32);
            e.Property(x => x.ModuloOrigem).HasMaxLength(64);
            e.Property(x => x.ReferenciaExterna).HasMaxLength(128);
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.IpOrigem).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.Metadados)
                .HasColumnType("jsonb")
                .HasConversion(metadadosConverter)
                .Metadata.SetValueComparer(
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, string>>(
                        (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
                        v => JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(
                                 JsonSerializer.Serialize(v, jsonOptions), jsonOptions)
                             ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
            e.HasIndex(x => x.EmpresaId);
            e.HasIndex(x => new { x.EmpresaId, x.Ativo });
            e.HasIndex(x => x.SistemaOrigem);
            e.HasIndex(x => x.ChecksumSha256);
        });

        base.OnModelCreating(modelBuilder);
    }
}
