using System;

namespace MiniHttpServer.Framework.Attributes;

/// <summary>
/// Задаёт HTTP-метод (GET, POST и т. д.), которым вызывается метод контроллера.
/// </summary>
public class MethodAttribute: Attribute
{
    /// <summary>
    /// Имя HTTP-метода.
    /// </summary>
    public string Method{get;}
    /// <summary>
    /// Создаёт атрибут для указанного HTTP-метода.
    /// </summary>
    /// <param name="method">Имя HTTP-метода, например <c>"GET"</c>.</param>
    public MethodAttribute(string method)
    {
        Method = method;
    }
    
}
