using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Handlers;

/// <summary>
/// Последний обработчик цепочки: отвечает 404, если запрос не обработал никто другой.
/// </summary>
public class NotFoundHandler : Handler
{
    /// <inheritdoc />
    public override Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
        => new HtmlResult("<h1>404 Not Found</h1>", 404).ExecuteAsync(context.Response, token);
}