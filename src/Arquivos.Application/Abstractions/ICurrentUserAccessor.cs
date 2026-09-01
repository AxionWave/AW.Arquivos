using Arquivos.Core.Auth;

namespace Arquivos.Application.Abstractions;

public interface ICurrentUserAccessor
{
    CurrentUser User { get; }
}
