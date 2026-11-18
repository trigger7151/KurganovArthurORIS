namespace MiniHttpServer.Framework.Authentication;

/// <summary>
/// Общие значения для аутентификации, которыми пользуются
/// и фреймворк (проверка [Authorize]), и приложение (вход и выход).
/// </summary>
public static class AuthenticationDefaults
{
    /// <summary>
    /// Имя Cookie, в которой хранится идентификатор пользовательского сеанса.
    /// </summary>
    public const string SessionCookieName = "sessionId";
}
