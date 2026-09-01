using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Arquivos.API.Filters;

/// <summary>Expõe no Swagger os headers usados em testes locais (chamada interna).</summary>
public sealed class EnterpriseHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Empresa-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Obrigatório na chamada interna. Ex.: 1",
            Schema = new OpenApiSchema { Type = "string" }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Origin-System",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Sigla do sistema de origem. Ex.: ASC",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
