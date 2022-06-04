namespace Play.Catalog.Service.Settings;

public class PgSettings {
    public string? Host { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Database { get; init; }
    public string ConnectionString => $"Host={Host};Username={Username};Password={Password};Database={Database}";
}