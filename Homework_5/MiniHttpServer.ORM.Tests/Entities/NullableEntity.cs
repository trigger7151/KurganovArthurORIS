namespace MiniHttpServer.ORM.Tests.Entities;

public class NullableEntity
{
    public int Id { get; set; }
    public int? Age { get; set; } = -1;
    public string? Email { get; set; } = "x";
}
