using System;
using System.Net;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Attributes;

namespace MiniHttpServer.Framework.Controllers;

public abstract class ControllerBase
{
    public HttpListenerContext? Context {get;set;}
    public Server? Server {get;set;}
    public CancellationToken? Token{get;set;}
    // public static IHttpResult ReturnFileResult(string path) => new FileResult(path);
    // public static IHttpResult ReturnStaticResult() => new StaticResult();

    public static IHttpResult Html(string html, int statusCode = 200) => new HtmlResult(html, statusCode);
    public static IHttpResult Json(object? data, int statusCode = 200) => new JsonResult(data, statusCode);

    public void Initialize(HttpListenerContext context, Server server, CancellationToken token)
    {
        Context = context;
        Server = server;
        Token = token;
    }
}
