namespace MiniHttpServer.Framework.Results;
public sealed class PageResult(
    string templatePath,
    object model,
    int statusCode = 200,
    IHtmlTemplateRenderer? renderer = null) : IHttpResult
{
    private static readonly string TemplatesRoot =
        Path.Combine(AppContext.BaseDirectory, "Templates");

    private readonly IHtmlTemplateRenderer _renderer = renderer ?? new HtmlTemplateRenderer();

    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(TemplatesRoot, templatePath);
        string html = _renderer.RenderFromFile(fullPath, model);
        await new HtmlResult(html, statusCode).ExecuteAsync(response, cancellationToken);
    }
}
