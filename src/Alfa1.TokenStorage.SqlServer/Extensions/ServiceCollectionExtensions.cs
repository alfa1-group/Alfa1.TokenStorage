using Alfa1.TokenStorage.Abstractions;
using Alfa1.TokenStorage.SqlServer;
using Alfa1.TokenStorage.SqlServer.Data;
using Alfa1.TokenStorage.SqlServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTokenStorageSqlServer(this IServiceCollection services, IConfiguration configuration, ServiceLifetime dbContextLifetime = ServiceLifetime.Scoped)
    {
        services.AddOptions<EntityFrameworkCoreTokenStorageOptions>()
            .Bind(configuration.GetSection("SqlServerTokenStorageOptions"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddDbContext<AuthenticationTokenDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(serviceProvider.GetOptions().ConnectionStringName);

            options.UseSqlServer(connectionString);
        }, dbContextLifetime);

        services.EnsureTokenTableExists();

        return dbContextLifetime switch
        {
            ServiceLifetime.Singleton => services.AddSingleton<ITokenStorageService, EntityFrameworkCoreTokenStorageService>(),
            ServiceLifetime.Scoped => services.AddScoped<ITokenStorageService, EntityFrameworkCoreTokenStorageService>(),
            _ => services.AddTransient<ITokenStorageService, EntityFrameworkCoreTokenStorageService>()
        };
    }

    private static EntityFrameworkCoreTokenStorageOptions GetOptions(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IOptions<EntityFrameworkCoreTokenStorageOptions>>().Value;
    }

    private static void EnsureTokenTableExists(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AuthenticationTokenDbContext>();

        dbContext.Database.EnsureCreated();
        dbContext.EnsureAuthenticationTokenTableExists();
    }
}