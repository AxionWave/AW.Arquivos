using Arquivos.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arquivos.API.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ArquivosException ax)
        {
            context.Result = new ObjectResult(new { error = ax.ErrorCode, message = ax.Message })
            {
                StatusCode = ax.StatusCode
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is FileNotFoundException)
        {
            context.Result = new ObjectResult(new
            {
                error = "blob_nao_encontrado",
                message = "O conteúdo deste arquivo não está mais no storage."
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
            context.ExceptionHandled = true;
        }
    }
}
