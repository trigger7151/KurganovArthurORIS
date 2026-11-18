using System.Net;
using System.Text.Json;
using System.IO.Compression;
using MiniHttpServer.Framework.Results;
namespace HttpServer.Controllers;

[Route("/cookie")]
public class CookieController : ControllerBase
{
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

