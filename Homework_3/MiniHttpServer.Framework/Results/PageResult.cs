namespace MiniHttpServer.Framework.Results;
/// <summary>
/// Ответ со страницей, построенной по HTML-шаблону: шаблон рендерится с моделью,
/// а готовый HTML отправляется через <see cref="HtmlResult"/>.
/// </summary>
/// <param name="templatePath">Путь к шаблону относительно каталога Templates.</param>
/// <param name="model">Модель данных для подстановки.</param>
/// <param name="statusCode">HTTP-статус ответа.</param>
/// <param name="renderer">Шаблонизатор; по умолчанию <see cref="HtmlTemplateRenderer"/>.</param>
public sealed class PageResult(
    string templatePath,
    object model,
    int statusCode = 200,
    IHtmlTemplateRenderer? renderer = null) : IHttpResult
{
    /// <summary>
    /// Каталог шаблонов в папке приложения.
    /// </summary>
    private static readonly string TemplatesRoot =
        Path.Combine(AppContext.BaseDirectory, "Templates");

    /// <summary>
    /// Шаблонизатор, которым рендерится страница.
    /// </summary>
    private readonly IHtmlTemplateRenderer _renderer = renderer ?? new HtmlTemplateRenderer();

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(TemplatesRoot, templatePath);
        string html = _renderer.RenderFromFile(fullPath, model);
        await new HtmlResult(html, statusCode).ExecuteAsync(response, cancellationToken);
    }
}
