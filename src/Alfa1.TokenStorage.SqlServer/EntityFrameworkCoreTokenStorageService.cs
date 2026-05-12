using System.Diagnostics.CodeAnalysis;
using Alfa1.TokenStorage.Abstractions;
using Alfa1.TokenStorage.SqlServer.Data;
using Alfa1.TokenStorage.SqlServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alfa1.TokenStorage.SqlServer;

internal class EntityFrameworkCoreTokenStorageService(
    ILogger<EntityFrameworkCoreTokenStorageService> logger,
    IOptions<EntityFrameworkCoreTokenStorageOptions> options,
    IMemoryCache memoryCache,
    AuthenticationTokenDbContext dbContext,
    TimeProvider timeProvider) : ITokenStorageService
{
    private static readonly Random RandomJitter = new();
    private const int MaxConcurrencyRetries = 3;
    private const int RetryTimeOutInMs = 1000;

    public async Task<string> StoreRefreshTokenAsync(string currentRefreshToken, string newRefreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentRefreshToken);
        ArgumentException.ThrowIfNullOrEmpty(newRefreshToken);

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var entity = await dbContext.Tokens.SingleOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                entity = new AuthenticationToken
                {
                    RefreshToken = newRefreshToken,
                    RefreshTokenUpdatedAt = timeProvider.GetUtcNow()
                };

                dbContext.Tokens.Add(entity);
            }
            else
            {
                // Only change if database still has the token we just used.
                if (entity.RefreshToken != currentRefreshToken)
                {
                    logger.LogInformation("Skipping refresh token update. Database already contains a newer refresh token (attempt {Attempt}).", attempt);
                    return entity.RefreshToken;
                }

                entity.RefreshToken = newRefreshToken;
                entity.RefreshTokenUpdatedAt = timeProvider.GetUtcNow();
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return newRefreshToken;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict while storing refresh token (attempt {Attempt}/{Max}).", attempt, MaxConcurrencyRetries);

                if (attempt == MaxConcurrencyRetries)
                {
                    throw;
                }

                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }

                await WaitAsync(cancellationToken);
            }
        }

        return newRefreshToken; // Should not reach here.
    }

    public async Task<string> RetrieveRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Tokens.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (entity == null)
        {
            logger.LogInformation("Token entity does not exist in table {Table}. Returning empty string for RefreshToken.", options.Value.TableName);
            return string.Empty;
        }

        return entity.RefreshToken;
    }

    public async Task<string> RetrieveAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetNonEmptyAccessTokenFromCache(out var accessToken))
        {
            return accessToken;
        }

        var entity = await dbContext.Tokens.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (entity == null)
        {
            logger.LogInformation("Token entity does not exist in table {Table}. Returning empty string for AccessToken.", options.Value.TableName);
            return string.Empty;
        }

        if (string.IsNullOrEmpty(entity.AccessToken))
        {
            logger.LogInformation("AccessToken is null or empty in table {Table}. Returning empty string for AccessToken.", options.Value.TableName);
            return string.Empty;
        }

        if (timeProvider.GetUtcNow() <= entity.AccessTokenExpire)
        {
            return memoryCache.Set(options.Value.AccessTokenColumnName, entity.AccessToken, entity.AccessTokenExpire);
        }

        logger.LogInformation("AccessToken is expired at {AccessTokenExpire}. Returning empty string value for AccessToken.", entity.AccessTokenExpire);
        return string.Empty;
    }

    public async Task<string> StoreAccessTokenAsync(string? currentAccessToken, string newAccessToken, TimeSpan absoluteExpirationRelativeToUtcNow, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(newAccessToken);

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var entity = await dbContext.Tokens.SingleOrDefaultAsync(cancellationToken);
            if (entity == null)
            {
                logger.LogInformation("Token entity does not exist in table {Table}. Returning empty string for AccessToken.", options.Value.TableName);
                return string.Empty;
            }

            // Skip update if:
            // - currentAccessToken is not null or empty
            // - database contains a newer access token than the one we just used
            if (!string.IsNullOrEmpty(currentAccessToken) && !string.IsNullOrEmpty(entity.AccessToken) && entity.AccessToken != currentAccessToken)
            {
                logger.LogInformation("Skipping access token update. Database already contains a newer access token (attempt {Attempt}).", attempt);

                // Add to cache if not present.
                if (!TryGetNonEmptyAccessTokenFromCache(out _))
                {
                    return memoryCache.Set(options.Value.AccessTokenColumnName, entity.AccessToken, entity.AccessTokenExpire);
                }

                return entity.AccessToken;
            }

            var now = timeProvider.GetUtcNow();
            entity.AccessToken = newAccessToken;
            entity.AccessTokenUpdatedAt = now;
            entity.AccessTokenExpire = now.Add(absoluteExpirationRelativeToUtcNow);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return memoryCache.Set(options.Value.AccessTokenColumnName, newAccessToken, absoluteExpirationRelativeToUtcNow);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict while storing access token (attempt {Attempt}/{Max}).", attempt, MaxConcurrencyRetries);

                if (attempt == MaxConcurrencyRetries)
                {
                    throw;
                }

                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }

                await WaitAsync(cancellationToken);
            }
        }

        return string.Empty; // Should not reach here.
    }

    private bool TryGetNonEmptyAccessTokenFromCache([NotNullWhen(true)] out string? accessToken)
    {
        if (memoryCache.TryGetValue(options.Value.AccessTokenColumnName, out accessToken))
        {
            return !string.IsNullOrEmpty(accessToken);
        }

        return false;
    }

    private static Task WaitAsync(CancellationToken cancellationToken)
    {
        // Wait some time (1000 ms) with a bit of random jitter (0-500 ms) to avoid synchronized retries.
        return Task.Delay(RetryTimeOutInMs + RandomJitter.Next(0, 500), cancellationToken);
    }
}