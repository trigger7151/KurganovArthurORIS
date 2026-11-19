using System.Net;
using MiniHttpServer.Framework.Results;
using MiniTemplateEngine;

namespace CardsApp.Web.Infrastructure;

/// <summary>
/// Страница из представления и общего макета.
/// Шаблонизатор рендерит макет (Shared/_Layout.html) и представление по отдельности,
/// затем готовый HTML представления подставляется в макет на место &lt;!--body--&gt;.
/// Так шапка и подключение стилей описаны один раз, а не копируются в каждый шаблон.
/// Слоты &lt;!--raw:Имя--&gt; заполняются уже очищенным HTML (описание из WYSIWYG-редактора),
/// который шаблонизатор иначе экранировал бы.
/// </summary>
public sealed class ViewResult(
    string viewPath,
    object model,
    LayoutVm layout,
    int statusCode = 200,
    IReadOnlyDictionary<string, string>? rawSlots = null,
    IHtmlTemplateRenderer? renderer = null) : IHttpResult
{
    public const string LayoutPath = "Shared/_Layout.html";
    private static readonly string TemplatesRoot = Path.Combine(AppContext.BaseDirectory, "Templates");
    private readonly IHtmlTemplateRenderer _renderer = renderer ?? new HtmlTemplateRenderer();

    public string Render()
    {
        var body = _renderer.RenderFromFile(Path.Combine(TemplatesRoot, viewPath), model);
        foreach (var (name, html) in rawSlots ?? new Dictionary<string, string>())
            body = body.Replace($"<!--raw:{name}-->", html);

        var page = _renderer.RenderFromFile(Path.Combine(TemplatesRoot, LayoutPath), layout);
        return page.Replace("<!--body-->", body);
    }

    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
        => new HtmlResult(Render(), statusCode).ExecuteAsync(response, cancellationToken);
}

/// <summary>Перенаправление (303 See Other — после POST браузер выполнит GET).</summary>
public sealed class RedirectResult(string location, int statusCode = 303) : IHttpResult
{
    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        response.StatusCode = statusCode;
        response.RedirectLocation = location;
        response.ContentLength64 = 0;
        return Task.CompletedTask;
    }
}
