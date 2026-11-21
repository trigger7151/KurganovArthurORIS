namespace CardsApp.Domain;

/// <summary>Текст эффекта на уровне наложения S1–S5. Таблица superimpositions.</summary>
public class SuperimpositionEntity : IEntity
{
    public int Id { get; set; }
    public int LightConeId { get; set; }
    public int Level { get; set; }
    public string Text { get; set; } = "";
}
