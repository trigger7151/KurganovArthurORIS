using System;
using System.Net;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Content;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;

namespace MiniHttpServer.Framework.Controllers;

/// <summary>
/// Базовый класс контроллеров. Даёт доступ к текущему запросу и сервису сеансов
/// и вспомогательные методы для создания результатов.
/// </summary>
public abstract class ControllerBase
{
    /// <summary>
    /// Контекст текущего HTTP-запроса.
    /// </summary>
    public HttpListenerContext? Context {get;set;}
    /// <summary>
    /// Сервер, который обрабатывает запрос.
    /// </summary>
    public Server? Server {get;set;}
    /// <summary>
    /// Общий сервис пользовательских сеансов.
    /// </summary>
    protected IUserSessionService SessionService
    {
        get;
        private set;
    } = null!;
    /// <summary>
    /// Токен отмены; срабатывает при остановке сервера.
    /// </summary>
    public CancellationToken? Token{get;set;}
    // public static IHttpResult ReturnFileResult(string path) => new FileResult(path);
    // public static IHttpResult ReturnStaticResult() => new StaticResult();


    /// <summary>
    /// Создаёт ответ с готовым HTML.
    /// </summary>
    /// <param name="html">Разметка страницы.</param>
    /// <param name="statusCode">HTTP-статус ответа.</param>
    /// <returns>Результат <see cref="HtmlResult"/>.</returns>
    protected static IHttpResult Html(string html, int statusCode = 200) => new HtmlResult(html, statusCode);
    /// <summary>
    /// Создаёт ответ с объектом, сериализованным в JSON.
    /// </summary>
    /// <param name="data">Объект для сериализации.</param>
    /// <param name="statusCode">HTTP-статус ответа.</param>
    /// <returns>Результат <see cref="JsonResult"/>.</returns>
    protected static IHttpResult Json(object? data, int statusCode = 200) => new JsonResult(data, statusCode);
    /// <summary>
    /// Создаёт ответ со страницей, построенной по HTML-шаблону.
    /// </summary>
    /// <param name="templatePath">Путь к шаблону относительно каталога Templates.</param>
    /// <param name="model">Модель данных для подстановки в шаблон.</param>
    /// <param name="statusCode">HTTP-статус ответа.</param>
    /// <returns>Результат <see cref="PageResult"/>.</returns>
    protected static IHttpResult Page(string templatePath, object model, int statusCode = 200) => new PageResult(templatePath, model, statusCode);

    /// <summary>
    /// Передаёт контроллеру данные текущего запроса. Вызывается <see cref="Handlers.RouteHandler"/>
    /// сразу после создания контроллера и до вызова его метода.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="server">Сервер, который обрабатывает запрос.</param>
    /// <param name="sessionService">Общий сервис сеансов.</param>
    /// <param name="token">Токен отмены.</param>
    public void Initialize(HttpListenerContext context, Server server, IUserSessionService sessionService, CancellationToken token)
    {
        Context = context;
        Server = server;
        SessionService = sessionService;
        Token = token;

    }
}
