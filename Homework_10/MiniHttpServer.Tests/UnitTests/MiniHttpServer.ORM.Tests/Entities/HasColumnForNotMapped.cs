using System;

namespace MiniHttpServer.ORM.Tests.Entities;

public class HasColumnForNotMapped
{
    public int Id {get;set;}

    [NotMapped]
    public int NotMapped {get;set;} = -1;
}
