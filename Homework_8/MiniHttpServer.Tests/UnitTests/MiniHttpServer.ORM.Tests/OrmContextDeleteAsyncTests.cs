using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiniHttpServer.ORM.Tests;

[TestClass]
public class OrmContextDeleteAsyncTests
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
    public async Task DeleteAsync_SqlInjectionInTableName_ThrowsArgumentEx(string tableName)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.DeleteAsync(tableName, 1));
    }

    [TestMethod]
    public async Task DeleteAsync_TableNameExceedsLengthLimit_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.DeleteAsync(new string('a', 64), 1));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task DeleteAsync_TableNameEmpty_ThrowsArgumentEx(string tableName)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.DeleteAsync(tableName, 1));
    }

    [TestMethod]
    public async Task DeleteAsync_TableNameNull_ThrowsArgumentNullEx()
    {
        var ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => Context.DeleteAsync(null!, 1));
        Assert.AreEqual("tableName", ex.ParamName);
    }

    [TestMethod]
    public async Task DeleteAsync_TableNameEndsWithLineBreak_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Context.DeleteAsync("TestTable\n", 1));
    }
}
