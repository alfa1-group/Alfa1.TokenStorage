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
        ConfigureDependencies(services, configuration, dbContextLifetime);

        return dbContextLifetime switch
        {
            ServiceLifetime.Singleton => services.AddSingleton<ITokenStorageService, EntityFrameworkCoreTokenStorageService>(),
            ServiceLifetime.Scoped => services.AddScoped<ITokenStorageService, EntityFrameworkCoreTokenStorageService>(),
            _ => services.AddTransient<ITokenStorageService, EntityFrameworkCoreTokenStorageService>()
        };
    }

    public static IServiceCollection AddTokenStorageSqlServerFor<TDependent>(this IServiceCollection services, IConfiguration configuration,
        string? tokenIdentifier = null, ServiceLifetime dbContextLifetime = ServiceLifetime.Scoped, ServiceLifetime dependentLifetime = ServiceLifetime.Scoped)
        where TDependent : class
    {
        var resolvedTokenIdentifier = string.IsNullOrWhiteSpace(tokenIdentifier) ? typeof(TDependent).Name : tokenIdentifier;

        services.AddKeyedTokenStorageSqlServer(typeof(TDependent), resolvedTokenIdentifier, configuration, dbContextLifetime);

        return dependentLifetime switch
        {
            ServiceLifetime.Singleton => services.AddSingleton<TDependent>(serviceProvider =>
                ActivatorUtilities.CreateInstance<TDependent>(serviceProvider,
                    serviceProvider.GetRequiredKeyedService<ITokenStorageService>(typeof(TDependent)))),
            ServiceLifetime.Scoped => services.AddScoped<TDependent>(serviceProvider =>
                ActivatorUtilities.CreateInstance<TDependent>(serviceProvider,
                    serviceProvider.GetRequiredKeyedService<ITokenStorageService>(typeof(TDependent)))),
            _ => services.AddTransient<TDependent>(serviceProvider =>
                ActivatorUtilities.CreateInstance<TDependent>(serviceProvider,
                    serviceProvider.GetRequiredKeyedService<ITokenStorageService>(typeof(TDependent))))
        };
    }

    public static IServiceCollection AddTokenStorageSqlServerFor<TService, TImplementation>(this IServiceCollection services, IConfiguration configuration,
        string? tokenIdentifier = null, ServiceLifetime dbContextLifetime = ServiceLifetime.Scoped, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        var resolvedTokenIdentifier = string.IsNullOrWhiteSpace(tokenIdentifier) ? typeof(TImplementation).Name : tokenIdentifier;

        services.AddKeyedTokenStorageSqlServer(typeof(TImplementation), resolvedTokenIdentifier, configuration, dbContextLifetime);

        return serviceLifetime switch
        {
            ServiceLifetime.Singleton => services.AddSingleton<TService>(serviceProvider =>
                ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider,
                    serviceProvider.GetRequiredKeyedService<ITokenStorageService>(typeof(TImplementation)))),
            ServiceLifetime.Scoped => services.AddScoped<TService>(serviceProvider =>
                ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider,
                    serviceProvider.GetRequiredKeyedService<ITokenStorageService>(typeof(TImplementation)))),
            _ => services.AddTransient<TService>(serviceProvider =>
                ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider,
                    serviceProvider.GetRequiredKeyedService<ITokenStorageService>(typeof(TImplementation))))
        };
    }

    public static IServiceCollection AddKeyedTokenStorageSqlServer(this IServiceCollection services, object serviceKey, string tokenIdentifier, IConfiguration configuration,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Scoped)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenIdentifier);

        ConfigureDependencies(services, configuration, dbContextLifetime);

        return dbContextLifetime switch
        {
            ServiceLifetime.Singleton => services.AddKeyedSingleton<ITokenStorageService>(serviceKey,
                (serviceProvider, _) => ActivatorUtilities.CreateInstance<EntityFrameworkCoreTokenStorageService>(serviceProvider, tokenIdentifier)),
            ServiceLifetime.Scoped => services.AddKeyedScoped<ITokenStorageService>(serviceKey,
                (serviceProvider, _) => ActivatorUtilities.CreateInstance<EntityFrameworkCoreTokenStorageService>(serviceProvider, tokenIdentifier)),
            _ => services.AddKeyedTransient<ITokenStorageService>(serviceKey,
                (serviceProvider, _) => ActivatorUtilities.CreateInstance<EntityFrameworkCoreTokenStorageService>(serviceProvider, tokenIdentifier))
        };
    }

    private static void ConfigureDependencies(IServiceCollection services, IConfiguration configuration, ServiceLifetime dbContextLifetime)
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