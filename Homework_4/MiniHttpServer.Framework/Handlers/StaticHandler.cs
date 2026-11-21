using System;
using System.Net;
using System.Reflection;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Routing;
using MiniHttpServer.Framework.Handlers;
namespace MiniHttpServer.Framework.Handlers;

/// <summary>
/// Обработчик статических файлов из каталога static. Путь URL отсчитывается от static;
/// для каталога отдаётся его index.html, а <c>/</c> — страница из настроек.
/// Пути за пределами static не обслуживаются. Если файла нет, запрос передаётся дальше.
/// </summary>
public class StaticHandler: Handler
{
    /// <inheritdoc />
    public override async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        if (!(context.Request.HttpMethod == "GET"))
        {
            await base.HandleAsync(context, server, token);
            return;
        }
        var userPath = context.Request.Url.LocalPath;
        var staticRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "static"));
        var fullFilePath   = Path.GetFullPath(Path.Combine(staticRoot, userPath.TrimStart('/')));
        if (userPath == "/" && File.Exists(server.ResponsePath))
        {
            await new StaticResult(server.ResponsePath).ExecuteAsync(context.Response, token);
            return;
        }

        if (!fullFilePath.StartsWith(staticRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            await Next.HandleAsync(context, server, token);
            return;
        }
        var fileExists = File.Exists(fullFilePath);
        var directoryExists = Directory.Exists(fullFilePath);

        if (directoryExists && File.Exists(fullFilePath + "/index.html"))
        {
            await new StaticResult(fullFilePath + "/index.html").ExecuteAsync(context.Response, token);
        }
        else if (fileExists)
        {
            await new StaticResult(fullFilePath).ExecuteAsync(context.Response, token);
        }
        else
        {
            await base.HandleAsync(context, server, token);
        }
    }
}
