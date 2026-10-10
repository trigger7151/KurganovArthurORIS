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

public delegate Task RouteProcessor(HttpListenerContext context, Server server, CancellationToken token);
