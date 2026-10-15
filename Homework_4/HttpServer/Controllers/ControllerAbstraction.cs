using System;
using System.Net;

using Search_server;

namespace HttpServer.Controllers;

public class RouteAttribute: Attribute
{
    public string Path{get;}
    public RouteAttribute(string path)
    {
        Path = path;
    }
}
public class MethodAttribute: Attribute
{
    public string Method{get;}
    public MethodAttribute(string method)
    {
        Method = method;
    }
    
}

public abstract class ControllerBase
{
    public HttpListenerContext? Context {get;set;}
    public Server Server {get;set;}
    public CancellationToken Token{get;set;}
    public ControllerBase()
    {
        
    }
}