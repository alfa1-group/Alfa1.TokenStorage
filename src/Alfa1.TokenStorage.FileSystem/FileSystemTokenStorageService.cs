using Alfa1.TokenStorage.Abstractions;
using Alfa1.TokenStorage.FileSystem.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alfa1.TokenStorage.FileSystem;

internal class FileSystemTokenStorageService(ILogger<FileSystemTokenStorageService> logger, IOptions<FileSystemTokenStorageOptions> options, IMemoryCache memoryCache) :
    ITokenStorageService
{
    private readonly string _refreshTokenFilePath = options.Value.RefreshTokenFilePath;

    public async Task<string> RetrieveRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_refreshTokenFilePath))
        {
            logger.LogInformation("RefreshToken file does not exist at path: {FilePath}. Returning empty string value.", _refreshTokenFilePath);
            return string.Empty;
        }

        return await File.ReadAllTextAsync(_refreshTokenFilePath, cancellationToken);
    }

    public async Task<string> StoreRefreshTokenAsync(string currentRefreshToken, string newRefreshToken, CancellationToken cancellationToken = default)
    {
        var existingRefreshToken = await RetrieveRefreshTokenAsync(cancellationToken);
        if (existingRefreshToken == currentRefreshToken)
        {
            await File.WriteAllTextAsync(_refreshTokenFilePath, newRefreshToken, cancellationToken);
            return newRefreshToken;
        }

        return existingRefreshToken;
    }

    public Task<string> RetrieveAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(options.Value.AccessTokenFilePath, out string? accessToken) && !string.IsNullOrEmpty(accessToken))
        {
            return Task.FromResult(accessToken);
        }

        return Task.FromResult(string.Empty);
    }

    public async Task<string> StoreAccessTokenAsync(string? currentAccessToken, string newAccessToken, TimeSpan absoluteExpirationRelativeToUtcNow, CancellationToken cancellationToken = default)
    {
        var existingAccessToken = await RetrieveAccessTokenAsync(cancellationToken);

        if (!string.IsNullOrEmpty(currentAccessToken) && existingAccessToken != currentAccessToken)
        {
            logger.LogWarning("The existing access token does not match the provided current access token. The access token will not be updated to avoid potential conflicts.");
            return existingAccessToken;
        }

        return memoryCache.Set(options.Value.AccessTokenFilePath, newAccessToken, absoluteExpirationRelativeToUtcNow);
    }
}