using System.Reflection;
using Npgsql;
using Play.Catalog.Service.Entities;

namespace Play.Catalog.Service.Repositories;

public class PostgreRepository<T> : IRepository<T> where T : IEntity
{
    private readonly string tableName;
    private readonly NpgsqlConnection dbConn;

    public PostgreRepository(NpgsqlConnection dbConn, string tableName)
    {
        this.dbConn = dbConn;
        this.tableName = tableName;
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        await dbConn.OpenAsync();

        await using NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM $1", dbConn) {
            Parameters = { new() { Value = tableName } }
        };
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        List<T> items = new();
        while (await reader.ReadAsync())
        {
            Type entityType = typeof(T);
            object? entity = Activator.CreateInstance(entityType);
            PropertyInfo[] property = entityType.GetProperties();

            for (int i = 0; i < property.Length; i++) {
                property[i].SetValue(entity, reader.GetValue(i));
            }
            
            T? en = (T?)entity;
            if (en != null)
                items.Add(en);
            // items.Add(new()
            // {
            //     Id = reader.GetGuid(0),
            //     Name = reader.GetString(1),
            //     Description = reader.GetString(2),
            //     Price = reader.GetDecimal(3),
            //     CreatedDate = reader.GetDateTime(4)
            // });
        }

        await dbConn.CloseAsync();
        return items;
    }

    public async Task<T?> GetAsync(Guid id)
    {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM $1 WHERE id=$2", dbConn)
        {
            Parameters = {
                new() { Value = tableName },
                new() { Value = id }
            }
        };

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        Type entityType = typeof(T);
        object? entity = Activator.CreateInstance(entityType);
        PropertyInfo[] property = entityType.GetProperties();

        for (int i = 0; i < property.Length; i++) {
            property[i].SetValue(entity, reader.GetValue(i));
        }
        
        T? en = (T?)entity;

        await dbConn.CloseAsync();
        return en;
    }

    public async Task CreateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException();

        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO item (name, description, price, created_date) VALUES ($1, $2, $3, $4) RETURNING id", dbConn)
        {
            Parameters = {
                new() { Value = entity.Name},
                new() { Value = entity.Description},
                new() { Value = entity.Price},
                new() { Value = entity.CreatedDate}
            }
        };
        string? entityId = (await cmd.ExecuteScalarAsync())?.ToString();
        await dbConn.CloseAsync();

        if (entityId == null) throw new NpgsqlException();
        entity.Id = Guid.Parse(entityId);
    }

    public async Task UpdateAsync(T entity)
    {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand("UPDATE item SET name=$1, description=$2, price=$3 WHERE id=$4", dbConn)
        {
            Parameters = {
                new() { Value = entity.Name },
                new() { Value = entity.Description },
                new() { Value = entity.Price },
                new() { Value = entity.Id }
            }
        };
        await cmd.ExecuteNonQueryAsync();
        await dbConn.CloseAsync();
    }

    public async Task DelteAsync(Guid id)
    {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM item WHERE id=$1", dbConn)
        {
            Parameters = {
                new() { Value = id}
            }
        };
        await cmd.ExecuteNonQueryAsync();
        await dbConn.CloseAsync();
    }
}