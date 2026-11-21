namespace MiniHttpServer.ORM.IntegrationTests.Entities;

public class InsertProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}
