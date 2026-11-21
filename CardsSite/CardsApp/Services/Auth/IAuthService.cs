namespace CardsApp.Services.Auth;

public interface IAuthService
{
    Task<UserEntity?> GetUserAsync(int? userId, CancellationToken ct = default);
    /// <summary>Возвращает пользователя или null, если логин или пароль неверны.</summary>
    Task<UserEntity?> SignInAsync(string? login, string? password, CancellationToken ct = default);
}
