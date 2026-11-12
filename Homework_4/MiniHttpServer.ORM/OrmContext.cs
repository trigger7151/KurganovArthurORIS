using System;
using System.Data;
using System.Text.RegularExpressions;
using Npgsql;
using System.Reflection;
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
        var properties = typeof(T).GetProperties();
        await using var connection = new NpgsqlConnection(ConnectionString);
        if (!Regex.IsMatch(tableName, @"^[A-Za-z_][A-Za-z0-9_]*\z"))
            throw new ArgumentException($"incorrect format of the table name: '{tableName}'");
        var command = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var propertyToColumn = GetPropertyToColumnDictionary(properties, reader);
        var a = reader.GetOrdinal("");
        var list = new List<T>{};
        if (!reader.HasRows)
            return list;

        while (await reader.ReadAsync(cancellationToken))
        {
            var instance = Activator.CreateInstance<T>();
            SetInstanceProperties(instance, reader, properties, propertyToColumn);
            list.Add(instance);
        }
        return list;
    }

    private static void SetInstanceProperties(object? instance, NpgsqlDataReader? reader, 
    PropertyInfo[]? properties, Dictionary<string, int>? propertyToColumn)
    {
        foreach (var property in properties)
        {
            var value = reader.GetValue(propertyToColumn[property.Name]);
            property.SetValue(instance, value);
        }
    }

    private static Dictionary<string, int> GetPropertyToColumnDictionary(PropertyInfo[]? properties, NpgsqlDataReader? reader)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties)
        {
            var column = reader.GetOrdinal(property.Name);
            dict[property.Name] = column;
        }
        return dict;
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
