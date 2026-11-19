using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Results;

// Фреймворк ищет контроллеры только в этом пространстве имён (RouteTable.MapRoutes):
// класс должен оканчиваться на Controller, а действия — начинаться с Process.
namespace HttpServer.Controllers;

/// <summary>Страница 1: каталог световых конусов (поиск, фильтры, сортировка).</summary>
[Route("/")]
public class CatalogController : AppController
{
    [Method("GET")]
    [Route("")]
    public Task<IHttpResult> ProcessIndex(string? q, string? rarity, string? path, string? sort)
        => SafeAsync(async () =>
        {
            var model = await Services.Catalog.GetCatalogAsync(new CatalogQuery(q, rarity, path, sort), Ct);
            return View("Catalog/Index.html", model, "Световые конусы", scripts: ["/js/catalog.js"]);
        });
}

/// <summary>Страница 2: карточка светового конуса. Адрес: /cone?id=1.</summary>
[Route("/cone")]
public class ConeController : AppController
{
    [Method("GET")]
    [Route("")]
    public Task<IHttpResult> ProcessDetails(string? id)
        => SafeAsync(async () =>
        {
            var model = await Services.Catalog.GetDetailsAsync(ParseId(id), Ct);
            if (model is null)
                return NotFoundPage("Световой конус не найден. Возможно, его удалили или адрес набран с ошибкой.");

            return View("Cone/Details.html", model, model.Name,
                raw: new Dictionary<string, string> { ["description"] = model.DescriptionHtml },
                scripts: ["/js/cone.js"], bodyClass: $"rarity-{model.Rarity}");
        });
}

/// <summary>JSON-эндпоинты для AJAX-запросов.</summary>
[Route("/api")]
public class ApiController : AppController
{
    /// <summary>Каталог без перезагрузки страницы: те же фильтры, что и на главной.</summary>
    [Method("GET")]
    [Route("cones")]
    public Task<IHttpResult> ProcessCones(string? q, string? rarity, string? path, string? sort)
        => SafeAsync(async () =>
        {
            var page = await Services.Catalog.GetCatalogAsync(new CatalogQuery(q, rarity, path, sort), Ct);
            return JsonResponse(new
            {
                success = true,
                countText = page.CountText,
                countShort = page.CountShort,
                isFiltered = page.IsFiltered,
                items = page.Items,
            });
        }, json: true);

    /// <summary>Проверка уникальности названия конуса прямо при вводе в админке.</summary>
    [Method("GET")]
    [Route("cones/name-available")]
    public Task<IHttpResult> ProcessNameAvailable(string? name, string? id)
        => SafeAsync(async () =>
        {
            var text = (name ?? "").Trim();
            if (text.Length is < 2 or > 80)
                return JsonResponse(new { success = true, available = false });
            var available = await Services.ConesAdmin.IsNameAvailableAsync(text, ParseId(id), Ct);
            return JsonResponse(new { success = true, available });
        }, json: true);
}
