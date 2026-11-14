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
            await base.Next.HandleAsync(context, server, token);
            //response.StatusCode = (int)HttpStatusCode.NotFound;
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
