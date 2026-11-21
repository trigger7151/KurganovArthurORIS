namespace CardsApp.Domain;

/// <summary>
/// Общий контракт сущности: у каждой таблицы есть первичный ключ Id.
/// ORM пропускает свойство Id при вставке и изменении записи.
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}
