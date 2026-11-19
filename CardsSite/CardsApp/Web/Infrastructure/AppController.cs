using System.Net;
using System.Collections.Specialized;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Routing;

namespace CardsApp.Web.Infrastructure;

/// <summary>
/// Базовый контроллер приложения: доступ к сервисам, рендеринг страниц,
/// текущий пользователь и единая обработка ошибок.
/// Лежит вне пространства имён HttpServer.Controllers, поэтому фреймворк
/// не принимает его за контроллер при построении таблицы маршрутов.
/// </summary>
public abstract class AppController : ControllerBase
{
    protected AppServices Services => AppServices.Current;
    protected CancellationToken Ct => Token ?? CancellationToken.None;
    protected HttpListenerRequest Request => Context!.Request;

    // ---------- Результаты ----------

    protected IHttpResult View(string viewPath, object model, string title, int statusCode = 200,
        IReadOnlyDictionary<string, string>? raw = null, string[]? scripts = null, string bodyClass = "",
        User? user = null, bool adminArea = false)
    {
        var layout = new LayoutVm
        {
            Title = title.Length > 0 ? $"{title} — Light Cone Wiki" : "Light Cone Wiki",
            BodyClass = bodyClass,
            IsAdminArea = adminArea,
            IsSignedIn = user is not null,
            UserName = user?.DisplayName ?? "",
            IsAdmin = user?.Role == UserRoles.Admin,
            Styles = adminArea ? ["/vendor/quill/quill.snow.css", "/css/site.css", "/css/admin.css"] : ["/css/site.css"],
            Scripts = (scripts ?? []).ToList(),
        };
        return new ViewResult(viewPath, model, layout, statusCode, raw);
    }

    protected static IHttpResult Redirect(string url) => new RedirectResult(url);

    protected IHttpResult MessagePage(int code, string title, string message, User? user = null, bool adminArea = false)
        => View("Errors/Message.html", new MessagePageVm { Code = code.ToString(), Title = title, Message = message },
            title, code, user: user, adminArea: adminArea);

    protected IHttpResult NotFoundPage(string message = "Такой страницы нет. Возможно, конус удалили или адрес набран с ошибкой.")
        => MessagePage(404, "Страница не найдена", message);

    protected static IHttpResult JsonResponse(object? data, int statusCode = 200) => Json(data, statusCode);

    /// <summary>
    /// Выполняет действие и превращает любое исключение (например, недоступную БД)
    /// в страницу ошибки со ссылкой на главную — сервер не падает и пользователь может продолжить работу.
    /// </summary>
    protected async Task<IHttpResult> SafeAsync(Func<Task<IHttpResult>> action, bool json = false)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (Ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ошибка] {Request.HttpMethod} {Request.Url?.PathAndQuery}: {ex}");
            if (json)
                return Json(new { success = false, message = "Внутренняя ошибка сервера. Попробуйте ещё раз." }, 500);
            try
            {
                return MessagePage(500, "Что-то пошло не так",
                    "Не удалось обработать запрос. Попробуйте обновить страницу чуть позже.");
            }
            catch (Exception renderError)
            {
                Console.WriteLine($"[Ошибка] страница ошибки не отрисовалась: {renderError.Message}");
                return Html("<!doctype html><meta charset=\"utf-8\"><h1>500</h1><p>Внутренняя ошибка сервера.</p><p><a href=\"/\">На главную</a></p>", 500);
            }
        }
    }

    // ---------- Пользователь и сеанс ----------

    protected string? SessionId => Request.Cookies[AuthenticationDefaults.SessionCookieName]?.Value;

    protected Task<User?> GetCurrentUserAsync()
        => Services.Auth.GetUserAsync(SessionService.GetUserId(SessionId), Ct);

    protected void SetSessionCookie(string sessionId)
        => Context!.Response.AppendHeader("Set-Cookie",
            $"{AuthenticationDefaults.SessionCookieName}={sessionId}; Path=/; HttpOnly; SameSite=Lax; " +
            $"Max-Age={(int)SessionService.SessionLifetime.TotalSeconds}");

    protected void ClearSessionCookie()
        => Context!.Response.AppendHeader("Set-Cookie",
            $"{AuthenticationDefaults.SessionCookieName}=; Path=/; HttpOnly; SameSite=Lax; Max-Age=0");

    // ---------- Данные формы ----------

    /// <summary>Все параметры запроса: строка запроса + тело формы (через ParameterBinder фреймворка).</summary>
    protected Task<NameValueCollection> ReadFormAsync() => ParameterBinder.GetRequestParametersAsync(Request);

    protected static int ParseId(string? value) => int.TryParse(value, out var id) && id > 0 ? id : 0;
}

/// <summary>Базовый контроллер админки: вход обязателен, иначе — перенаправление на форму входа.</summary>
public abstract class AdminAreaController : AppController
{
    protected IHttpResult AdminView(string viewPath, object model, string title, User user, int statusCode = 200,
        string[]? scripts = null)
        => View(viewPath, model, title, statusCode, scripts: scripts, user: user, adminArea: true, bodyClass: "admin");

    /// <summary>
    /// Выполняет действие от имени вошедшего пользователя.
    /// Без входа — редирект на /admin/login; не хватает прав — страница 403.
    /// </summary>
    protected Task<IHttpResult> AuthorizedAsync(Func<User, Task<IHttpResult>> action, bool adminOnly = false)
        => SafeAsync(async () =>
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
            {
                var returnUrl = Request.HttpMethod == "GET" ? Request.Url?.PathAndQuery ?? "/admin" : "/admin";
                return Redirect("/admin/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
            }
            if (adminOnly && user.Role != UserRoles.Admin)
                return MessagePage(403, "Недостаточно прав",
                    "Это действие доступно только администратору. Обратитесь к нему или войдите под другой учётной записью.",
                    user, adminArea: true);
            return await action(user);
        });
}
