namespace CardsApp.Domain;

/// <summary>
/// Общий контракт сущности: у каждой таблицы есть первичный ключ Id.
/// ORM пропускает свойство Id при вставке и изменении записи.
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}

/// <summary>Путь (класс) светового конуса: Разрушение, Охота и т. д. Таблица paths.</summary>
public class CombatPath : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Латинский ключ пути, используется в адресах фильтра: ?path=destruction.</summary>
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>Световой конус — коллекционная карточка. Таблица lightcones.</summary>
public class LightCone : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Редкость: 3, 4 или 5 звёзд.</summary>
    public int Rarity { get; set; }
    public int PathId { get; set; }
    /// <summary>Название пассивного эффекта, например «Всем приготовиться».</summary>
    public string EffectName { get; set; } = "";
    /// <summary>Путь к арту 902×1260 (/uploads/... или внешний URL). Может отсутствовать.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Описание в HTML из WYSIWYG-редактора. Перед сохранением очищается от опасных тегов.</summary>
    public string DescriptionHtml { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

/// <summary>Базовые характеристики конуса на определённом уровне. Таблица lightconestats.</summary>
public class LightConeStat : IEntity
{
    public int Id { get; set; }
    public int LightConeId { get; set; }
    public int Level { get; set; }
    public int Hp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
}

/// <summary>Текст эффекта на уровне наложения S1–S5. Таблица superimpositions.</summary>
public class Superimposition : IEntity
{
    public int Id { get; set; }
    public int LightConeId { get; set; }
    public int Level { get; set; }
    public string Text { get; set; } = "";
}

/// <summary>Пользователь административной панели. Таблица users.</summary>
public class User : IEntity
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>admin — полный доступ; editor — без удаления и без управления путями.</summary>
    public string Role { get; set; } = UserRoles.Editor;
}

public static class UserRoles
{
    public const string Admin = "admin";
    public const string Editor = "editor";
}

public static class StatLevels
{
    public const int Min = 1;
    public const int Max = 80;
}
