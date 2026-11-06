using System;
using System.Net;
using HttpServer.Results;
using Search_server;
using HttpServer.Framework.Attributes;
using HttpServer.Results;

namespace HttpServer.Framework;

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
