namespace CardsApp.Domain;

/// <summary>Путь (класс) светового конуса: Разрушение, Охота и т. д. Таблица paths.</summary>
public class CombatPathEntity : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Латинский ключ пути, используется в адресах фильтра: ?path=destruction.</summary>
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
}
