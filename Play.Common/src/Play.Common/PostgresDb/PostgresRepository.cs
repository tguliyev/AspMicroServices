using Npgsql;

namespace Play.Common.PostgresDb;
public class PostgreRepository<T> : IRepository<T> where T : IEntity
{
    private readonly NpgsqlConnection dbConn;

    public PostgreRepository(NpgsqlConnection dbConn)
    {
        this.dbConn = dbConn;
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        await dbConn.OpenAsync();

        await using NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM item", dbConn);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        List<T> items = new();
        while (await reader.ReadAsync())
        {
            items.Add(new()
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CreatedDate = reader.GetDateTime(4)
            });
        }

        await dbConn.CloseAsync();
        return items;
    }

    public async Task<T?> GetAsync(Guid id)
    {
        await dbConn.OpenAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM item WHERE id=$1", dbConn)
        {
            Parameters = {
                new() { Value = id }
            }
        };

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        T item = new Item()
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            Description = reader.GetString(2),
            Price = reader.GetDecimal(3),
            CreatedDate = reader.GetDateTime(4)
        };
        await dbConn.CloseAsync();
        return item;
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