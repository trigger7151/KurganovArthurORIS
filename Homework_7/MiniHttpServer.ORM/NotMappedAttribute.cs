namespace MiniHttpServer.ORM;

/// <summary>
/// Помечает свойство, которое не хранится в базе данных.
/// ORM не ищет для него столбец и не заполняет его.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotMappedAttribute : Attribute
{
}
