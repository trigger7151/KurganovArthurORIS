using System;
using System.Data;
using System.Text.RegularExpressions;
using Npgsql;
namespace MiniHttpServer.ORM;

public partial class OrmContext(string connectionString) : IOrmContext
{
    private string ConnectionString { get; set; } = connectionString;
    
    public async Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken = default) where T : class, new()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        if (!Regex.IsMatch(tableName, @"^[A-Za-z_][A-Za-z0-9_]*\z"))
            throw new ArgumentException($"incorrect format of the table name: '{tableName}'");
        var command = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<T>{};
        if (!reader.HasRows)
            return list;

        while (await reader.ReadAsync(cancellationToken))
        {
            
        }
        throw new NotImplementedException();
    }

    public async Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default) where T : class
    {
        throw new NotImplementedException();
    }

    public async Task<int> UpdateAsync<T>(string tableName, int id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        throw new NotImplementedException();
    }
}
