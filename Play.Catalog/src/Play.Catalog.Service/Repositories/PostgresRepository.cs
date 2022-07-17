using System.Reflection;
using Npgsql;
using Play.Catalog.Service.Entities;

namespace Play.Catalog.Service.Repositories;

public class PostgreRepository<T> : IRepository<T> where T : IEntity
{
    private readonly string tableName;
    private readonly NpgsqlConnection dbConn;

    public PostgreRepository(NpgsqlConnection dbConn, string tableName) {
        this.dbConn = dbConn;
        this.tableName = tableName;
    }

    public async Task<IReadOnlyCollection<T?>> GetAllAsync() {
        await dbConn.OpenAsync();

        await using NpgsqlCommand cmd = new($"SELECT * FROM {tableName}", dbConn);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        List<T?> items = new();
        while (await reader.ReadAsync()) {
            Type entityType = typeof(T);
            object? entity = Activator.CreateInstance(entityType);
            PropertyInfo[] property = entityType.GetProperties();

            for (int i = 0; i < property.Length; i++) {
                property[i].SetValue(entity, reader.GetValue(i));
            }
            
            items.Add((T?)entity);
        }

        await dbConn.CloseAsync();
        return items;
    }

    public async Task<T?> GetAsync(Guid id) {
        await dbConn.OpenAsync();
        // Console.WriteLine(tableName);
        await using NpgsqlCommand cmd = new($"SELECT * FROM {tableName} WHERE id=$1", dbConn) {
            Parameters = { new() { Value = id } }
        };

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        Type entityType = typeof(T);
        object? entity = Activator.CreateInstance(entityType);
        PropertyInfo[] property = entityType.GetProperties();

        for (int i = 0; i < property.Length; i++) {
            property[i].SetValue(entity, reader.GetValue(i));
        }

        await dbConn.CloseAsync();
        return (T?)entity;
    }

    public async Task CreateAsync(T entity) {
        if (entity == null) throw new ArgumentNullException();

        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand($"INSERT INTO {tableName} (name, description, price, created_date) VALUES ($1, $2, $3, $4) RETURNING id", dbConn);
        Type entityType = typeof(T);
        PropertyInfo[] property = entityType.GetProperties();
        for (int i = 1; i < property.Length; i++) {
            object? let = property[i].GetValue(entity);
            cmd.Parameters.Add(new() { Value =  let });
        }

        string? entityId = (await cmd.ExecuteScalarAsync())?.ToString();
        await dbConn.CloseAsync();

        if (entityId == null) throw new NpgsqlException();
        entity.Id = Guid.Parse(entityId);
    }

    public async Task UpdateAsync(T entity) {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand($"UPDATE {tableName} SET name=$1, description=$2, price=$3 WHERE id=$4", dbConn);
        Type entityType = typeof(T);
        PropertyInfo[] property = entityType.GetProperties();
        for (int i = 1; i < property.Length - 1; i++) {
            object? let = property[i].GetValue(entity);
            cmd.Parameters.Add(new() { Value =  let });
        }
        cmd.Parameters.Add( new() { Value = property[0].GetValue(entity) });
        await cmd.ExecuteNonQueryAsync();
        await dbConn.CloseAsync();
    }

    public async Task DelteAsync(Guid id) {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand($"DELETE FROM {tableName} WHERE id=$1", dbConn) {
            Parameters = { new() { Value = id} }
        };
        await cmd.ExecuteNonQueryAsync();
        await dbConn.CloseAsync();
    }
}