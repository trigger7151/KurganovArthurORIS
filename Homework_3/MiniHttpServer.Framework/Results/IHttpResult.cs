using System.Net;
namespace MiniHttpServer.Framework.Results;

/// <summary>
/// Результат действия контроллера: умеет записать себя в HTTP-ответ
/// (статус, заголовки и тело). Контроллер только создаёт результат,
/// а выполняет его <see cref="Handlers.RouteHandler"/>.
/// </summary>
public interface IHttpResult
{
    /// <summary>
    /// Записывает результат в HTTP-ответ: статус, заголовки и тело.
    /// Закрывать ответ не нужно — это делает сервер.
    /// </summary>
    /// <param name="response">HTTP-ответ текущего запроса.</param>
    /// <param name="cancellationToken">Токен отмены записи.</param>
    /// <returns>Задача, завершающаяся после записи ответа.</returns>
    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default);
}
