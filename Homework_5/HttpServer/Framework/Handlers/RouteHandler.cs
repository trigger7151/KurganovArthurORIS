using System;
using System;
using System.Net;
using System.Reflection;
using MimeTypesSpace;
using Search_server;
using HttpServer.Controllers;
using HttpServer.Results;
using HttpServer.Framework.Attributes;
using HttpServer.Framework;
using HttpServer.Framework.Routing;
using HttpServer.Handlers;
namespace HttpServer.Framework.Handlers;

public class RouteHandler: Handler
{
    public override async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        var localUrl = context.Request.Url.AbsolutePath.Trim('/');
        localUrl = string.IsNullOrEmpty(localUrl) ? "/" : "/" + localUrl;
        Console.WriteLine($"Относительный url {localUrl}");
        
        var routeEntry = RouteTable.Routes[(context.Request.HttpMethod, localUrl)];
        var controller = (ControllerBase)Activator.CreateInstance(routeEntry.ControllerType);
        controller.Context = context;
        controller.Server = server;
        controller.Token = token;
        var result = await (Task<IResult>)routeEntry.Method.Invoke(controller, null);
        await result.Execute(context);
    }
}
