using System.Collections.Concurrent;

namespace MiniHttpServer.Framework.Authentication;

/// <summary>
/// Реализует управление пользовательскими сеансами
/// с хранением данных в оперативной памяти.
/// </summary>
public sealed class UserSessionService : IUserSessionService
{
    /// <summary>Время жизни сеанса по умолчанию.</summary>
    private static readonly TimeSpan DefaultSessionLifetime = TimeSpan.FromMinutes(30);
    /// <summary>Число одновременных сеансов пользователя по умолчанию.</summary>
    private const int DefaultMaxSessionsPerUser = 5;

    /// <summary>
    /// Сеансы по идентификатору; потокобезопасен, так как запросы обрабатываются параллельно.
    /// </summary>
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
    /// <summary>Сколько сеансов одновременно может быть у пользователя.</summary>
    private readonly int _maxSessionsPerUser;

    /// <summary>Создаёт сервис сеансов.</summary>
    /// <param name="sessionLifetime">
    /// Время жизни сеанса без активности. По умолчанию 30 минут.
    /// </param>
    /// <param name="maxSessionsPerUser">
    /// Сколько сеансов одновременно может быть у пользователя.
    /// При превышении удаляется самый давно неактивный. По умолчанию 5.
    /// </param>
    public UserSessionService(TimeSpan? sessionLifetime = null, int maxSessionsPerUser = DefaultMaxSessionsPerUser)
    {
        SessionLifetime = sessionLifetime ?? DefaultSessionLifetime;

        if (SessionLifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(sessionLifetime), "Время жизни сеанса должно быть положительным.");
        if (maxSessionsPerUser < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSessionsPerUser), "Пользователю должен быть доступен хотя бы один сеанс.");

        _maxSessionsPerUser = maxSessionsPerUser;
    }

    /// <inheritdoc />
    public TimeSpan SessionLifetime { get; }

    /// <inheritdoc />
    public string CreateSession(int userId)
    {
        RemoveExpiredSessions();
        RemoveExcessSessions(userId);

        string sessionId = Guid.NewGuid().ToString("N");
        var session = new SessionInfo(userId, DateTimeOffset.UtcNow.Add(SessionLifetime));

        if (!_sessions.TryAdd(sessionId, session))
        {
            throw new InvalidOperationException("Не удалось создать уникальный сеанс.");
        }

        return sessionId;
    }

    /// <inheritdoc />
    public int? GetUserId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        if (!_sessions.TryGetValue(sessionId, out SessionInfo? session))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (session.ExpiresAt <= now)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }


        var renewed = session with { ExpiresAt = now.Add(SessionLifetime) };
        if (_sessions.TryUpdate(sessionId, renewed, session))
        {
            return session.UserId;
        }

        return _sessions.ContainsKey(sessionId) ? session.UserId : null;
    }

    /// <inheritdoc />
    public void RemoveSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        _sessions.TryRemove(sessionId, out _);
    }

    /// <inheritdoc />
    public void RemoveUserSessions(int userId)
    {
        // ConcurrentDictionary разрешает удалять элементы во время перебора
        foreach (var (sessionId, session) in _sessions)
        {
            if (session.UserId == userId)
            {
                _sessions.TryRemove(sessionId, out _);
            }
        }
    }

    /// <summary>
    /// Удаляет все истёкшие сеансы, чтобы они не копились в памяти.
    /// </summary>
    private void RemoveExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (sessionId, session) in _sessions)
        {
            if (session.ExpiresAt <= now)
            {
                _sessions.TryRemove(sessionId, out _);
            }
        }
    }

    /// <summary>
    /// Освобождает место под новый сеанс: если у пользователя уже
    /// максимум сеансов, удаляет самые давно неактивные.
    /// </summary>
    private void RemoveExcessSessions(int userId)
    {
        var userSessions = _sessions
            .Where(pair => pair.Value.UserId == userId)
            .OrderBy(pair => pair.Value.ExpiresAt)   // раньше истекает = дольше не было активности
            .ToList();

        int excess = userSessions.Count - (_maxSessionsPerUser - 1);
        foreach (var pair in userSessions.Take(excess))
        {
            _sessions.TryRemove(pair.Key, out _);
        }
    }

    /// <summary>
    /// Содержит информацию о пользовательском сеансе.
    /// </summary>
    /// <param name="UserId">
    /// Идентификатор пользователя.
    /// </param>
    /// <param name="ExpiresAt">
    /// Момент окончания действия сеанса.
    /// </param>
    private sealed record SessionInfo(int UserId, DateTimeOffset ExpiresAt);
}
