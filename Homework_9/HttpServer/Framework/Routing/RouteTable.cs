using System;
using System.Net;
using System.Reflection;
using MimeTypesSpace;
using Search_server;
using HttpServer.Controllers;
using HttpServer.Results;
using HttpServer.Framework.Attributes;
using HttpServer.Framework;
namespace HttpServer.Framework.Routing;

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
