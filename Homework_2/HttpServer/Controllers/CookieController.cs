using System.Net;
using System.Text.Json;
using System.IO.Compression;
using MiniHttpServer.Framework.Results;
namespace HttpServer.Controllers;

/// <summary>
/// Демонстрация операций с HTTP Cookie: установка, чтение и удаление тестовой Cookie.
/// </summary>
[Route("/cookie")]
public class CookieController : ControllerBase
{
    /// <summary>
    /// GET /cookie/set — устанавливает Cookie <c>testCookie</c> со сроком действия 30 минут.
    /// </summary>
    /// <returns>HTML-страница с подтверждением.</returns>
    [Method("GET")]
    [Route("/set")]
    public async Task<IHttpResult> ProcessSet()
    {
        var cookie = new Cookie(
            "testCookie",
            "HelloWorld",
            "/")
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        Context.Response.SetCookie(cookie);

        return Html("<h1>Cookie установлена</h1>");
    }

    /// <summary>
    /// GET /cookie/get — читает значение Cookie <c>testCookie</c> из запроса.
    /// </summary>
    /// <returns>JSON вида <c>{ "value": ... }</c>; значение равно null, если Cookie отсутствует.</returns>
    [Method("GET")]
    [Route("/get")]
    public async Task<IHttpResult> ProcessGet()
    {
        string? value =
            Context.Request.Cookies["testCookie"]?.Value;

        return Json(new
        {
            Value = value
        });
    }

    /// <summary>
    /// GET /cookie/delete — удаляет Cookie <c>testCookie</c>, отправляя её со сроком действия в прошлом.
    /// </summary>
    /// <returns>HTML-страница с подтверждением.</returns>
    [Method("GET")]
    [Route("/delete")]
    public async Task<IHttpResult> ProcessDelete()
    {
        var cookie = new Cookie(
            "testCookie",
            string.Empty,
            "/")
        {
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        Context.Response.SetCookie(cookie);

        return Html("<h1>Cookie удалена</h1>");
    }
}

