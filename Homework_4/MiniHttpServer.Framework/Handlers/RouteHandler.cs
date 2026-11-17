using System;
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

public class RouteHandler: Handler
{
    public override async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        var localUrl = context.Request.Url.LocalPath.Trim('/').ToLowerInvariant();
        localUrl = string.IsNullOrEmpty(localUrl) ? "/" : "/" + localUrl;
        Console.WriteLine($"Относительный url {localUrl}");
        // по пути находится объект RouteEntry. Объект хранит тип контроллера и method info для конкретного запроса по конкретному rest методу
        if (!RouteTable.Routes.TryGetValue((context.Request.HttpMethod, localUrl), out var routeEntry))
        {
            await Next.HandleAsync(context, server, token);
            return;
        }
        // создаем экземпляр этого контроллера
        var controller = (ControllerBase)Activator.CreateInstance(routeEntry.ControllerType);
        controller.Initialize(context, server, token);
        object?[] parameters;
        try
        {
            parameters = await ParameterBinder.BindAsync(routeEntry.Method, context.Request);
        }
        catch (ParameterBindingException ex)
        {
            await new JsonResult(new { Success = false, Message = ex.Message }, 400)
                .ExecuteAsync(context.Response, token);
            return;
        }
        // вызываем метод для контроллера и пытаемся привести к Task<IHttpResult>
        var result = await (Task<IHttpResult>)routeEntry.Method.Invoke(controller, parameters);
        await result.ExecuteAsync(context.Response, token);
    }
}
