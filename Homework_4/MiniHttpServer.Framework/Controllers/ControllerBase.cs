using System;
using System.Net;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;

namespace MiniHttpServer.Framework.Controllers;

public abstract class ControllerBase
{
    public HttpListenerContext? Context {get;set;}
    public Server? Server {get;set;}
    protected IUserSessionService SessionService
    {
        get;
        private set;
    } = null!;
    public CancellationToken? Token{get;set;}
    // public static IHttpResult ReturnFileResult(string path) => new FileResult(path);
    // public static IHttpResult ReturnStaticResult() => new StaticResult();


    protected static IHttpResult Html(string html, int statusCode = 200) => new HtmlResult(html, statusCode);
    protected static IHttpResult Json(object? data, int statusCode = 200) => new JsonResult(data, statusCode);
    protected static IHttpResult Page(string templatePath, object model, int statusCode = 200) => new PageResult(templatePath, model, statusCode);

    public void Initialize(HttpListenerContext context, Server server, IUserSessionService sessionService, CancellationToken token)
    {
        Context = context;
        Server = server;
        SessionService = sessionService;
        Token = token;

    }
}
