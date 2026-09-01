namespace Arquivos.Core.Exceptions;

public abstract class ArquivosException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    protected ArquivosException(string errorCode, int statusCode, string message) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public sealed class ArquivoNotFoundException() : ArquivosException(
    "arquivo_nao_encontrado", 404, "Arquivo não encontrado para este token.");

public sealed class ArquivoDesativadoException() : ArquivosException(
    "arquivo_desativado", 410, "Este arquivo foi desativado e não pode ser baixado.");

public sealed class TenantAccessDeniedException(string message) : ArquivosException(
    "acesso_negado", 403, message);

public sealed class ArquivoValidationException(string message) : ArquivosException(
    "validacao", 400, message);
