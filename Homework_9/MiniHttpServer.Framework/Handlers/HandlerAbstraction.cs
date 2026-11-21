using System;
using System.Net;
using MiniHttpServer.Framework.Content;
namespace MiniHttpServer.Framework.Handlers;

/// <summary>
/// Звено цепочки обработчиков запроса (паттерн «цепочка обязанностей»):
/// обработчик либо отвечает на запрос сам, либо передаёт его следующему.
/// </summary>
public interface IHandler
{
    /// <summary>
    /// Назначает следующий обработчик цепочки.
    /// </summary>
    /// <param name="handler">Следующий обработчик.</param>
    /// <returns>Переданный обработчик, чтобы цепочку можно было собирать подряд вызовами.</returns>
    IHandler SetNext(IHandler handler);
    /// <summary>
    /// Обрабатывает запрос или передаёт его следующему обработчику.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="server">Сервер, который обрабатывает запрос.</param>
    /// <param name="token">Токен отмены; срабатывает при остановке сервера.</param>
    /// <returns>Задача, завершающаяся после обработки.</returns>
    Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token);
}
/// <summary>
/// Базовая реализация звена цепочки: по умолчанию передаёт запрос следующему обработчику.
/// </summary>
public abstract class Handler : IHandler
{
    /// <summary>
    /// Следующий обработчик цепочки.
    /// </summary>
    public IHandler Next{get; private set;}
    /// <inheritdoc />
    public virtual async Task HandleAsync(HttpListenerContext context, Server server, CancellationToken token)
    {
        await Next.HandleAsync(context, server, token);
    }
    /// <inheritdoc />
    public virtual IHandler SetNext(IHandler handler)
    {
        Next = handler;
        return Next;
    }
}