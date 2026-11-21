namespace MiniHttpServer.ORM.IntegrationTests.Entities;

public class InsertSecretEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Secret { private get; set; } = "";
}
