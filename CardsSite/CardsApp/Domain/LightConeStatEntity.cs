namespace CardsApp.Domain;

/// <summary>Базовые характеристики конуса на определённом уровне. Таблица lightconestats.</summary>
public class LightConeStatEntity : IEntity
{
    public int Id { get; set; }
    public int LightConeId { get; set; }
    public int Level { get; set; }
    public int Hp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
}
