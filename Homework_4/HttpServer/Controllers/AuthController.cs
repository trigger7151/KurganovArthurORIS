using System.Net;
using MiniHttpServer.Framework.Authentication;

namespace HttpServer.Controllers;

/// <summary>
/// Вход и выход пользователя (практика 10).
/// </summary>
[Route("/auth")]
public class AuthController : ControllerBase
{
    // Учебная учётная запись — только для локальной демонстрации.
    // Для реальных пользователей нужна проверка хеша пароля из базы.
    private const string TestLogin = "student";
    private const string TestPassword = "test123";
    private const int TestUserId = 1;

    /// <summary>
    /// POST /auth/login — проверяет логин и пароль, создаёт сеанс и ставит Cookie.
    /// </summary>
    [Method("POST")]
    [Route("/login")]
    public async Task<IHttpResult> ProcessLogin(string? login, string? password)
    {
        if (login != TestLogin || password != TestPassword)
        {
            return Json(new { Success = false, Message = "Неверный логин или пароль" }, 401);
        }

        string sessionId = SessionService.CreateSession(TestUserId);


        var cookie = new Cookie(AuthenticationDefaults.SessionCookieName, sessionId, "/")
        {
            HttpOnly = true,
            Secure = Context!.Request.IsSecureConnection
        };
        Context.Response.SetCookie(cookie);

        return Json(new { Success = true, Message = "Вход выполнен" });
    }

    /// <summary>
    /// POST /auth/logout — удаляет сеанс на сервере и Cookie в браузере.
    /// </summary>
    [Method("POST")]
    [Route("/logout")]
    public async Task<IHttpResult> ProcessLogout()
    {
        string? sessionId = Context!.Request.Cookies[AuthenticationDefaults.SessionCookieName]?.Value;

        SessionService.RemoveSession(sessionId);


        var cookie = new Cookie(AuthenticationDefaults.SessionCookieName, string.Empty, "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
        Context.Response.SetCookie(cookie);

        return Json(new { Success = true, Message = "Выход выполнен" });
    }
}
