using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Handlers;

public class NotFoundHandler : Handler
{
    public override Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
        => new HtmlResult("<h1>404 Not Found</h1>", 404).ExecuteAsync(context.Response, token);
}