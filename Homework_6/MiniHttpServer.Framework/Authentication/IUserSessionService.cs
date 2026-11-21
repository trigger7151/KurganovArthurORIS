namespace MiniHttpServer.Framework.Authentication;

/// <summary>
/// Определяет контракт для управления
/// пользовательскими сеансами.
/// </summary>
public interface IUserSessionService
{
    /// <summary>
    /// Время жизни сеанса без активности.
    /// Используется и для срока действия Cookie,
    /// чтобы Cookie и сеанс истекали одновременно.
    /// </summary>
    TimeSpan SessionLifetime { get; }

    /// <summary>
    /// Создаёт новый сеанс пользователя.
    /// </summary>
    /// <param name="userId">
    /// Идентификатор пользователя.
    /// </param>
    /// <returns>
    /// Уникальный идентификатор созданного сеанса.
    /// </returns>
    string CreateSession(int userId);

    /// <summary>
    /// Получает идентификатор пользователя
    /// по действующему сеансу и продлевает сеанс.
    /// </summary>
    /// <param name="sessionId">
    /// Идентификатор сеанса.
    /// </param>
    /// <returns>
    /// Идентификатор пользователя или null,
    /// если сеанс отсутствует либо истёк.
    /// </returns>
    int? GetUserId(string? sessionId);

    /// <summary>
    /// Завершает пользовательский сеанс.
    /// </summary>
    /// <param name="sessionId">
    /// Идентификатор сеанса.
    /// </param>
    void RemoveSession(string? sessionId);

    /// <summary>
    /// Завершает все сеансы пользователя
    /// (например, после смены пароля).
    /// </summary>
    /// <param name="userId">
    /// Идентификатор пользователя.
    /// </param>
    void RemoveUserSessions(int userId);
}
