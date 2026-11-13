namespace MiniHttpServer.ORM.Tests;
using MiniHttpServer.ORM;

internal class TestTable
{
    
}

[TestClass]
public sealed class OrmContextTests
{
    private OrmContext Context {get;set;}
    private readonly string TestTable = "testTable";

    [TestInitialize]
    public async Task SetUp()
    {
        Context = new OrmContext("Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password");
    }
    
    [TestMethod]
    public async Task GetAllAsync_SqlInjectionInTableName_ThrowsArgumentEx()
    {
        
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameExceedsLengthLimit_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.GetAllAsync<TestTable>(new string('a', 64)));
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameEmpty_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.GetAllAsync<TestTable>(""));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.GetAllAsync<TestTable>("  "));
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameNull_ThrowsArgumentNullEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => Context.GetAllAsync<TestTable>(null));
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameEndsWithLineBreak_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.GetAllAsync<TestTable>("TestTable\\n"));
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnCountGreaterThanPropertyCount_CorrectResult()
    {
        
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnForNotMappedPropertyMissing_CorrectResult()
    {
        
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnForNotMappedPropertyFound_IgnoresColumn()
    {
        
    }

    [TestMethod]
    public async Task GetAllAsync_ColumnFoundForPropertyWithoutPublicSetter_ThrowsInvalidOperationEx()
    {
        
    }

    [TestMethod]
    public async Task GetAllAsync_CantCastColumnValueToPropertyType_ThrowsInvalidOperationEx()
    {
        
    }


}
