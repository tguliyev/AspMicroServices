using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Play.Common;
using Play.Common.Settings;

namespace Play.Common.PostgresDb;
public static class Extensions {
    public static IServiceCollection AddPostgres<T>(this IServiceCollection services, string tableName)
        where T : IEntity {
        services.AddSingleton(serviceProvider => {
            IConfiguration? configuration = serviceProvider.GetService<IConfiguration>();
            PgSettings? settings = configuration?.GetSection(nameof(PgSettings)).Get<PgSettings>();
            return new NpgsqlConnection(settings?.ConnectionString);
        });
        services.AddSingleton<IRepository<T>>(serviceProvider => {
            NpgsqlConnection? dbConn = serviceProvider.GetService<NpgsqlConnection>();
            
            if (dbConn == null)
                throw new ArgumentNullException();

            return new PostgreRepository<T>(dbConn, tableName);
        });
        return services;
    }
}