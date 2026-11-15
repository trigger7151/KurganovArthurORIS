using System;
using System.Net;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Attributes;

namespace MiniHttpServer.Framework.Controllers;

public abstract class ControllerBase
{
    public HttpListenerContext? Context {get;set;}
    public Server Server {get;set;}
    public CancellationToken Token{get;set;}
    public static IResult ReturnFileResult(string path) => new FileResult(path);
    public static IResult ReturnStaticResult() => new StaticResult();
    public ControllerBase()
    {
        
    }
}
