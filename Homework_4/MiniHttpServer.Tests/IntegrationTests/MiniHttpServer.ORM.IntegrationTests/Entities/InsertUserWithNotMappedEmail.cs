namespace MiniHttpServer.ORM.IntegrationTests.Entities;

public class InsertUserWithNotMappedEmail
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [NotMapped]
    public string Email { get; set; } = "";
}
