namespace HttpServer;

using System;
/// <summary>
/// Данные для входа в почтовый ящик отправителя, читаемые из AuthInfo.json.
/// </summary>
public class AuthenticationInfo
{
    /// <summary>
    /// Адрес почтового ящика отправителя.
    /// </summary>
    public string Email { get; set; }
    /// <summary>
    /// Пароль (пароль приложения) почтового ящика отправителя.
    /// </summary>
    public string Password { get; set; }
}
