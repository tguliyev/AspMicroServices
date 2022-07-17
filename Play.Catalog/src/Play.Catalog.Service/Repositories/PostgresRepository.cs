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

    public async Task<IReadOnlyCollection<T>> GetAllAsync() {
        await dbConn.OpenAsync();

        await using NpgsqlCommand cmd = new($"SELECT * FROM {tableName}", dbConn);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        List<T> items = new();
        while (await reader.ReadAsync()) {
            T entity = DataToEntity(reader);     
            items.Add(entity);
        }

        await dbConn.CloseAsync();
        return items;
    }

    public async Task<T> GetAsync(Guid id) {
        await dbConn.OpenAsync();

        await using NpgsqlCommand cmd = new($"SELECT * FROM {tableName} WHERE id=$1", dbConn) {
            Parameters = { new() { Value = id } }
        };
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        T entity = DataToEntity(reader);

        await dbConn.CloseAsync();
        return entity;
    }

    public async Task CreateAsync(T entity) {
        if (entity == null) throw new ArgumentNullException();
        await dbConn.OpenAsync();

        await using NpgsqlCommand cmd = new NpgsqlCommand($"INSERT INTO {tableName} (name, description, price) VALUES ($1, $2, $3) RETURNING id", dbConn);

        EntityToData(cmd, entity);

        string? entityId = (await cmd.ExecuteScalarAsync())?.ToString();
        entity.Id = Guid.Parse(entityId ?? throw new NpgsqlException());

        await dbConn.CloseAsync();
    }

    public async Task UpdateAsync(T entity) {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand($"UPDATE {tableName} SET name=$1, description=$2, price=$3 WHERE id=$4", dbConn);
        
        EntityToData(cmd, entity);

        cmd.Parameters.Add( new() { Value = typeof(T).GetProperty("Id")?.GetValue(entity) });
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

    private T DataToEntity(NpgsqlDataReader reader) {
        T entity = Activator.CreateInstance<T>();
        PropertyInfo[] property = typeof(T).GetProperties();

        for (int i = 0; i < property.Length; i++) {
            property[i].SetValue(entity, reader.GetValue(i));
        }

        return entity;
    }

    private void EntityToData(NpgsqlCommand cmd, T entity) {
        PropertyInfo[] property = typeof(T).GetProperties();

        for (int i = 1; i < property.Length - 1; i++) {
            object? data = property[i].GetValue(entity);
            Console.WriteLine(data);
            cmd.Parameters.Add(new() { Value = data ?? throw new ArgumentException() });
        }
    }
}