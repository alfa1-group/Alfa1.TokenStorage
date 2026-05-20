using Alfa1.TokenStorage.Abstractions;
using Azure.Storage.Blobs;
using Alfa1.TokenStorage.Azure.Blobs;
using Alfa1.TokenStorage.Azure.Blobs.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTokenStorageAzureBlobs(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureDependencies(services, configuration);

        return services.AddSingleton<ITokenStorageService, AzureBlobsTokenStorageService>();
    }

    public static IServiceCollection AddTokenStorageAzureBlobsFor<TDependent>(this IServiceCollection services, IConfiguration configuration,
        string? tokenIdentifier = null, ServiceLifetime dependentLifetime = ServiceLifetime.Scoped)
        where TDependent : class
    {
        var resolvedTokenIdentifier = string.IsNullOrWhiteSpace(tokenIdentifier) ? typeof(TDependent).Name : tokenIdentifier;

        services.AddKeyedTokenStorageAzureBlobs(typeof(TDependent), resolvedTokenIdentifier, configuration);

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

    public static IServiceCollection AddTokenStorageAzureBlobsFor<TService, TImplementation>(this IServiceCollection services, IConfiguration configuration,
        string? tokenIdentifier = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        var resolvedTokenIdentifier = string.IsNullOrWhiteSpace(tokenIdentifier) ? typeof(TImplementation).Name : tokenIdentifier;

        services.AddKeyedTokenStorageAzureBlobs(typeof(TImplementation), resolvedTokenIdentifier, configuration);

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

    public static IServiceCollection AddKeyedTokenStorageAzureBlobs(this IServiceCollection services, object serviceKey, string tokenIdentifier, IConfiguration configuration,
        ServiceLifetime storageLifetime = ServiceLifetime.Singleton)
    {
        if (string.IsNullOrEmpty(tokenIdentifier))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(tokenIdentifier));
        }

        ConfigureDependencies(services, configuration);

        return storageLifetime switch
        {
            ServiceLifetime.Singleton => services.AddKeyedSingleton<ITokenStorageService>(serviceKey,
                (serviceProvider, _) => ActivatorUtilities.CreateInstance<AzureBlobsTokenStorageService>(serviceProvider, tokenIdentifier)),
            ServiceLifetime.Scoped => services.AddKeyedScoped<ITokenStorageService>(serviceKey,
                (serviceProvider, _) => ActivatorUtilities.CreateInstance<AzureBlobsTokenStorageService>(serviceProvider, tokenIdentifier)),
            _ => services.AddKeyedTransient<ITokenStorageService>(serviceKey,
                (serviceProvider, _) => ActivatorUtilities.CreateInstance<AzureBlobsTokenStorageService>(serviceProvider, tokenIdentifier))
        };
    }

    private static void ConfigureDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureBlobsTokenStorageOptions>()
            .Bind(configuration.GetSection(nameof(AzureBlobsTokenStorageOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureBlobsTokenStorageOptions>>();
            return new BlobContainerClient(options.Value.ConnectionString, options.Value.ContainerName);
        });
    }
}