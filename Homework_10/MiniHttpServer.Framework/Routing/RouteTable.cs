using System;
using System.Net;
using System.Reflection;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
namespace MiniHttpServer.Framework.Routing;

/// <summary>
/// Запись таблицы маршрутов: какой метод какого контроллера вызывать.
/// </summary>
public class RouteEntry
{
    /// <summary>
    /// Тип контроллера.
    /// </summary>
    public Type ControllerType{get;private set;}
    /// <summary>
    /// Вызываемый метод контроллера.
    /// </summary>
    public MethodInfo Method{get; private set;}
    /// <summary>
    /// Создаёт запись маршрута.
    /// </summary>
    /// <param name="controllerType">Тип контроллера.</param>
    /// <param name="method">Вызываемый метод контроллера.</param>
    public RouteEntry(Type controllerType, MethodInfo method)
    {
        ControllerType = controllerType;
        Method = method;
    }
}

/// <summary>
/// Таблица маршрутов приложения.
/// </summary>
public static class RouteTable
{
    /// <summary>
    /// Маршруты по ключу «HTTP-метод в верхнем регистре, путь в нижнем регистре».
    /// </summary>
    public static Dictionary<(string Method, string Path), RouteEntry> Routes {get;} = new();
    /// <summary>
    /// Находит в сборке приложения контроллеры из пространства имён <c>HttpServer.Controllers</c>
    /// и регистрирует их методы, имена которых начинаются с <c>Process</c>.
    /// Полный путь маршрута складывается из <see cref="Attributes.RouteAttribute"/> класса и метода;
    /// метод HTTP берётся из <see cref="Attributes.MethodAttribute"/>.
    /// Вызывается один раз при сборке сервера в <see cref="ServerClasses.ServerService"/>.
    /// </summary>
    public static void MapRoutes()
    {
        // получаем все типы контроллеров
        var controllers = Assembly.GetEntryAssembly()!
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
                Routes[(httpMethod.ToUpperInvariant(), fullPath.ToLowerInvariant())] = routeEntry;
            }
        }
    }
}
