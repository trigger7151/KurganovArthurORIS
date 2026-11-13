using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;
using MiniHttpServer.ORM.Tests.Entities;
using Npgsql;
namespace Company.TestProject1;

[TestClass]
[DoNotParallelize]
public class OrmContextIntegrationTests
{
    private OrmContext Context {get; set;}

    [TestInitialize]
    public void SetUp()
    {
        Context = new OrmContext("Host=localhost;Port=5432;Database=OrisTestdb;Username=OrisTestUser;Password=bwydf175");
    }

    [ClassInitialize]
    public static async Task DbInitialize(TestContext context)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        string sql = "INSERT INTO users (name, email, age) VALUES ('Oleg',  'oleg@example.com', 22)";
        await using var command = new NpgsqlCommand(sql, connection);
        int rows = await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Добавлено строк: {rows}");
    }

    [TestMethod]
    public async Task GetAllAsync_TableName63Symbols_CorrectResult()
    {
        var result = await Context.GetAllAsync<LongNameEntity>(new string('a', 63));
        Assert.AreEqual(new List<LongNameEntity>(), result);
    }


}
