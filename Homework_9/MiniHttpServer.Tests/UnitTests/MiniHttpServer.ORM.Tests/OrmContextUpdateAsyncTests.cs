using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM.Tests.Entities;

namespace MiniHttpServer.ORM.Tests;

[TestClass]
public class OrmContextUpdateAsyncTests
{
    private OrmContext Context { get; set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        // несуществующий сервер: проверки имени таблицы должны срабатывать до подключения
        Context = new OrmContext("Host=abc;Port=5432;Database=gtx;Username=thk;Password=abc");
    }

    [TestMethod]
    [DataRow("users; DROP TABLE users")]
    [DataRow("users\"; DROP TABLE users; --")]
    [DataRow("users' OR '1'='1")]
    [DataRow("users--")]
    [DataRow("public.users")]
    public async Task UpdateAsync_SqlInjectionInTableName_ThrowsArgumentEx(string tableName)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.UpdateAsync(tableName, 1, new Product()));
    }

    [TestMethod]
    public async Task UpdateAsync_TableNameExceedsLengthLimit_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.UpdateAsync(new string('a', 64), 1, new Product()));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task UpdateAsync_TableNameEmpty_ThrowsArgumentEx(string tableName)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.UpdateAsync(tableName, 1, new Product()));
    }

    [TestMethod]
    public async Task UpdateAsync_TableNameNull_ThrowsArgumentNullEx()
    {
        var ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => Context.UpdateAsync(null!, 1, new Product()));
        Assert.AreEqual("tableName", ex.ParamName);
    }

    [TestMethod]
    public async Task UpdateAsync_TableNameEndsWithLineBreak_ThrowsArgumentEx()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => Context.UpdateAsync("TestTable\n", 1, new Product()));
    }

    [TestMethod]
    public async Task UpdateAsync_EntityNull_ThrowsArgumentNullEx()
    {
        var ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => Context.UpdateAsync<Product>("products", 1, null!));
        Assert.AreEqual("entity", ex.ParamName);
    }
}
