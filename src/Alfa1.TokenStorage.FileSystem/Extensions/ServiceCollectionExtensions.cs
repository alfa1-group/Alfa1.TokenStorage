using Alfa1.TokenStorage.Abstractions;
using Alfa1.TokenStorage.FileSystem;
using Alfa1.TokenStorage.FileSystem.Options;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTokenStorageFileSystem(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileSystemTokenStorageOptions>()
            .Bind(configuration.GetSection(nameof(FileSystemTokenStorageOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();

        return services.AddSingleton<ITokenStorageService, FileSystemTokenStorageService>();
    }
}