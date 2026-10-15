using System;
using System.Net;
using System.Reflection;
using MimeTypesSpace;
using Search_server;
using HttpServer.Controllers;
namespace HttpServer.Handlers;



public class RouteEntry
{
    public Type ControllerType{get;private set;}
    public MethodInfo Method{get; private set;}
    public RouteEntry(Type controllerType, MethodInfo method)
    {
        ControllerType = controllerType;
        Method = method;
    }
}

public static class RouteTable
{
    public static Dictionary<(string Method, string Path), RouteEntry> Routes {get;} = new();
    // вызывается один раз в Search_server.ServerService.Build()
    public static void MapRoutes()
    {
        // получаем все типы контроллеров
        var controllers = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(type => type.Namespace == "HttpServer.Controllers" 
        && type.Name.EndsWith("Controller") 
        && type.IsClass);

        foreach (var controller in controllers)
        {
            var path = controller.GetCustomAttribute<RouteAttribute>().Path;
            var processMethods = controller
            .GetMethods()
            .Where(method => method.Name.StartsWith("Process"));
            foreach (var method in processMethods)
            {
                var httpMethod = method.GetCustomAttribute<MethodAttribute>().Method;
                var methodPath = method.GetCustomAttribute<RouteAttribute>().Path;
                var fullPath = "/" + $"{path.TrimEnd('/')}/{methodPath.TrimStart('/')}".Trim('/');
                var routeEntry = new RouteEntry(controller, method);
                Routes[(httpMethod, fullPath)] = routeEntry;
            }
        }
    }
}
public class StaticHandler: Handler
{
    public override async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        if (!(context.Request.HttpMethod == "GET"))
        {
            await base.Next.HandleAsync(context, server, token);
            return;
        }
        var response = context.Response;
        var request = context.Request;
        byte[] buffer = new byte[0];
        var localUrl = request.Url.LocalPath;
        Console.WriteLine($"Относительный url {localUrl}");
        var url = Path.Combine(AppContext.BaseDirectory + "static", localUrl.TrimStart('/'));
        try
        {
            Console.WriteLine("путь запроса " + url);
            if(localUrl == "/")
            {
                buffer = await File.ReadAllBytesAsync($"{server.ResponsePath}", token);
                response.ContentType = "text/html; charset=utf-8";
                Console.WriteLine($"Стандартный адрес {server.ResponsePath}");
            }
            else
            {
                buffer = await File.ReadAllBytesAsync($"{url}", token);
                response.ContentType = MimeTypes.GetMimeType(url);
            }
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer, token);
        }
        catch (OperationCanceledException){}
        catch (IOException)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        } 
        catch (System.UnauthorizedAccessException)
        {
            await base.Next.HandleAsync(context, server, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
        }
    }
}

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
        await (Task)routeEntry.Method.Invoke(controller, null);
    }
}


