namespace MiniHttpServer.ORM.Tests;
using MiniHttpServer.ORM;
[TestClass]
public sealed class OrmContextTests
{
    private OrmContext context{get;set;}
    
    [TestMethod]
    public void GetAllAsync_SqlInjectionInTableName_throwsArgumentEx()
    {
        
    }
}
