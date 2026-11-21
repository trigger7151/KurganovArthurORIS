namespace CardsApp.Services.Auth;

public class AuthService(IUserRepository users, IPasswordHasher hasher) : IAuthService
{
    public async Task<UserEntity?> GetUserAsync(int? userId, CancellationToken ct = default)
        => userId is int id ? await users.GetByIdAsync(id, ct) : null;

    public async Task<UserEntity?> SignInAsync(string? login, string? password, CancellationToken ct = default)
    {
        login = (login ?? "").Trim();
        password ??= "";
        if (!ValidationRules.Login().IsMatch(login) || password.Length is 0 or > 64)
            return null;

        var user = await users.GetByLoginAsync(login, ct);
        return user is not null && hasher.Verify(password, user.PasswordHash) ? user : null;
    }
}
