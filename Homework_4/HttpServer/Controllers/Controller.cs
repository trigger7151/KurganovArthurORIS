using System;
using Search_server;
using System.Net;
using System.Web;
namespace HttpServer.Controllers;

[Route("/login")]
public class LoginController
{
    [Method("POST")]
    [Route("")]
    public async Task ProcessLoginPostAuth(HttpListenerContext context, Server server, CancellationToken token)
    {
        var request = context.Request;
        var response = context.Response;
        byte[] buffer = new byte[0];
        if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/login")
        {
            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                body = reader.ReadToEnd();

            var form = HttpUtility.ParseQueryString(body);

            string? login    = form["username"];
            string? password = form["password"];
            bool remember    = form["remember"] != null;
            string[]? langs  = form.GetValues("lang[]");    

            Console.WriteLine($"login={login}, password={password}, remember={remember}");

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer, token);
        }
    }
    [Method("GET")]
    [Route("")]
    public async Task ProcessLoginGet(HttpListenerContext context, Server server, CancellationToken token)
    {
        var response = context.Response;
        var request = context.Request;
        byte[] buffer = new byte[0];
        var localUrl = request.Url.LocalPath;
        Console.WriteLine($"Относительный url {localUrl}");
        var url = Path.Combine(AppContext.BaseDirectory + "static", localUrl.TrimStart('/'));
        buffer = await File.ReadAllBytesAsync($"{server.ResponsePath}", token);
        response.ContentType = "text/html; charset=utf-8";
        using Stream output = response.OutputStream;
        await output.WriteAsync(buffer, token);
    }
}
