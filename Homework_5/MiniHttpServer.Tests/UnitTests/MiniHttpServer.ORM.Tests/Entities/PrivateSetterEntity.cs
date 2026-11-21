namespace MiniHttpServer.ORM.Tests.Entities;

public class PrivateSetterEntity
{
    public int Id { get; set; }
    public int NoPublicSet { get; private set; }
}
