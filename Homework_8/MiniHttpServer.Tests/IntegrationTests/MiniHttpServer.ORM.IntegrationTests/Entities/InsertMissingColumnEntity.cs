namespace MiniHttpServer.ORM.IntegrationTests.Entities;

public class InsertMissingColumnEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Missing { get; set; } = "";
}
