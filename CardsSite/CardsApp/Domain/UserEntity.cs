namespace CardsApp.Domain;

/// <summary>Пользователь административной панели. Таблица users.</summary>
public class UserEntity : IEntity
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>Роль пользователя. Сейчас есть только admin — полный доступ к админке.</summary>
    public string Role { get; set; } = UserRoles.Admin;
}
