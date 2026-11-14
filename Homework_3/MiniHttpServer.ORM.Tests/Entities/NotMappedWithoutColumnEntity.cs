namespace MiniHttpServer.ORM.Tests.Entities;

public class NotMappedWithoutColumnEntity
{
    public int Id { get; set; }

    [NotMapped]
    public string Missing { get; set; } = "default";
}
