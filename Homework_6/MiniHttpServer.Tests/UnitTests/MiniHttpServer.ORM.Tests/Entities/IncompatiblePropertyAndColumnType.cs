using System;

namespace MiniHttpServer.ORM.Tests.Entities;

public class IncompatiblePropertyAndColumnType
{
    public int Id {get;set;}
    public int Incompatible {get;set;}
}
