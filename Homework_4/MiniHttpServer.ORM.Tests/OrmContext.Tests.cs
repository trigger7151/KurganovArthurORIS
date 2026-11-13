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
        Context = new OrmContext("Host=abc;Port=5432;Database=gtx;Username=thk;Password=password");
    }
    
    [TestMethod]
    [DataRow("users; DROP TABLE users")]
    [DataRow("users\"; DROP TABLE users; --")]
    [DataRow("users' OR '1'='1")]
    [DataRow("users--")]
    [DataRow("public.users")]
    public async Task GetAllAsync_SqlInjectionInTableName_ThrowsArgumentEx(string tableName)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.GetAllAsync<TestTable>(tableName));
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameExceedsLengthLimit_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.GetAllAsync<TestTable>(new string('a', 64)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task GetAllAsync_TableNameEmpty_ThrowsArgumentEx(string tableName)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.GetAllAsync<TestTable>(tableName));
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameNull_ThrowsArgumentNullEx()
    {
        var ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => Context.GetAllAsync<TestTable>(null!));
        Assert.AreEqual("tableName", ex.ParamName);
    }

    [TestMethod]
    public async Task GetAllAsync_TableNameEndsWithLineBreak_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.GetAllAsync<TestTable>("TestTable\n"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("abc")]                                    
    [DataRow("Hots=localhost;Database=orisdb")]          
    [DataRow("Database=orisdb")]                         
    [DataRow("Host=localhost")]                          
    public void Constructor_InvalidConnectionString_ThrowsArgumentEx(string connectionString)
    {
        Assert.Throws<ArgumentException>(() => new OrmContext(connectionString));
    }

    [TestMethod]
    public void Constructor_NullConnectionString_ThrowsArgumentNullEx()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new OrmContext(null!));
    }

    [TestMethod]
    [DataRow("Host=localhost;Database=orisdb;Username=postgres;Password=1")]
    [DataRow("Database=orisdb;Host=localhost")]                    
    [DataRow("Server=localhost;DB=orisdb")]                        
    public void Constructor_ValidConnectionString_DoesNotThrow(string connectionString)
    {
        _ = new OrmContext(connectionString);
    }
}
