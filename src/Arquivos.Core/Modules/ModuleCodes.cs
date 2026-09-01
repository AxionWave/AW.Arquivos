namespace Arquivos.Core.Modules;

/// <summary>
/// Códigos de módulo do serviço de arquivos. Devem existir em core.modulos.
/// O CRUD de arquivos NÃO exige estes módulos — qualquer JWT Enterprise válido
/// (ou chamada service-to-service) pode usar a API, isolada por empresaId.
/// O módulo raiz serve para catálogo ASC / administração.
/// </summary>
public static class ModuleCodes
{
    public const string Raiz = "ARQUIVOS000000";
    public const string Gestao = "ARQ0000001";

    public static readonly string[] Todos = [Raiz, Gestao];
}
