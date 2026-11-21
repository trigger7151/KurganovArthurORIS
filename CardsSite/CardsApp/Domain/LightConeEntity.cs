namespace CardsApp.Domain;

/// <summary>Световой конус — коллекционная карточка. Таблица lightcones.</summary>
public class LightConeEntity : IEntity
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
