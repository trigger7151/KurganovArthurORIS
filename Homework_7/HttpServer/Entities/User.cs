using System;

namespace HttpServer.Entities;

/// <summary>
/// Пользователь приложения.
/// </summary>
public class User
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }
 
    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string Name { get; set; } = string.Empty;
 
    /// <summary>
    /// Адрес электронной почты.
    /// </summary>
    public string Email { get; set; } = string.Empty;
 
    /// <summary>
    /// Возраст пользователя.
    /// </summary>
    public int Age { get; set; }

}
