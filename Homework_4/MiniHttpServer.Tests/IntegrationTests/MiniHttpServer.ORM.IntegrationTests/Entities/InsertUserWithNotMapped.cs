namespace MiniHttpServer.ORM.IntegrationTests.Entities;

public class InsertUserWithNotMapped
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [NotMapped]
    public string Nickname { get; set; } = "";
}
