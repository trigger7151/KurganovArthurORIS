using System;
using System.Net;
using System.Reflection;
using MimeTypesSpace;
using Search_server;
using HttpServer.Controllers;
namespace HttpServer.Handlers;

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
    Dictionary<(string Method, string Path), RouteProcessor> Routes { get; } = new();
    public void MapRoutes()
    {
        var controllers = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(type => type.Namespace == "HttpServer.Controllers" 
        && type.Name.EndsWith("Controller") 
        && type.IsClass);

        var routeAttributes = controllers
        .Select(controller => controller.GetCustomAttributes<RouteAttribute>());
        var paths = routeAttributes.SelectMany(attributes => attributes.Select(attribute => attribute)).Select(attribute => attribute.Path); 
        

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
                var instance = Activator.CreateInstance(controller);          // для нестатических методов
                Routes[(fullPath, httpMethod)] = method.CreateDelegate<RouteProcessor>(instance);
            }
        }
    }
    public override async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        if (Routes.Count == 0)
        {
            MapRoutes();
        }
        var localUrl = context.Request.Url.AbsolutePath.Trim('/');
        localUrl = string.IsNullOrEmpty(localUrl) ? "/" : "/" + localUrl;
        Console.WriteLine($"Относительный url {localUrl}");
        
        await Routes[(localUrl, context.Request.HttpMethod)](context, server, token);
    }
}
