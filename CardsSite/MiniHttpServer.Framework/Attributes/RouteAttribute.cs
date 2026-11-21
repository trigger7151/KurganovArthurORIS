using System;

namespace MiniHttpServer.Framework.Attributes;

/// <summary>
/// Задаёт часть пути маршрута. На контроллере — общий префикс,
/// на методе — продолжение пути после префикса.
/// </summary>
public class RouteAttribute: Attribute
{
    /// <summary>
    /// Часть пути маршрута.
    /// </summary>
    public string Path{get;}
    /// <summary>
    /// Создаёт атрибут с указанной частью пути.
    /// </summary>
    /// <param name="path">Часть пути, например <c>"/user"</c> или <c>"/profile"</c>.</param>
    public RouteAttribute(string path)
    {
        Path = path;
    }
}
