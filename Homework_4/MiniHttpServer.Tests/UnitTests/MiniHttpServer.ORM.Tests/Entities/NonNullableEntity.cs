namespace MiniHttpServer.ORM.Tests.Entities;

public class NonNullableEntity
{
    public int Id { get; set; }
    public int Age { get; set; }
    public string? Email { get; set; }
}
