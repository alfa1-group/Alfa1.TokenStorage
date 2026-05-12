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

        return services.AddSingleton<ITokenStorageService, AzureBlobsTokenStorageService>();
    }
}