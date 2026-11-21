using Npgsql;
namespace MiniHttpServer.ORM.IntegrationTests;

[TestClass]
public sealed class OrmContextDeleteAsyncTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=OrisTestdb;Username=OrisTestUser;Password=password";
    private OrmContext Context{get;set;}
    private const string NoIdColumnTable = "noidcolumntable";
    private const string IdNumDoesntExistTable = "idnumdoesntexisttable";

    private static async Task<int> ExecuteNonQueryAsync(string query)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await using var command = new NpgsqlCommand(query, connection);
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    

    [TestInitialize]
    public async Task DbInitialize()
    {
        Context = new OrmContext(ConnectionString);

        await ExecuteNonQueryAsync($"""
        drop table if exists {NoIdColumnTable};
        create table {NoIdColumnTable}
        (
            Idd serial primary key,
            Column1 integer
        );
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {IdNumDoesntExistTable};
        create table {IdNumDoesntExistTable}
        (
            Id serial primary key,
            Column1 integer
        );
        insert into {IdNumDoesntExistTable} (Column1)
        values (67);
        """);
    }

    [ClassCleanup]
    public static async Task DbCleanup()
    {
        await ExecuteNonQueryAsync($"""
        drop table if exists {NoIdColumnTable};
        """);

        await ExecuteNonQueryAsync($"""
        drop table if exists {IdNumDoesntExistTable};
        """);
    }

    

    [TestMethod]
    public async Task DeleteAsync_ColumnIdDoesntExist_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Context.DeleteAsync(NoIdColumnTable, 1));
    }

    [TestMethod]
    public async Task DeleteAsync_IdNumDoesntExist_ReturnsZero()
    {
        Assert.AreEqual(0, await Context.DeleteAsync(IdNumDoesntExistTable, 2));
    }

    [TestMethod]
    public async Task DeleteAsync_IdNumExists_ReturnsOne()
    {
        Assert.AreEqual(1, await Context.DeleteAsync(IdNumDoesntExistTable, 1));
    }
}
