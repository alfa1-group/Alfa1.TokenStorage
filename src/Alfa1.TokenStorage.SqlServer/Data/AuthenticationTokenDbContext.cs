using Alfa1.TokenStorage.SqlServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alfa1.TokenStorage.SqlServer.Data;

public partial class AuthenticationTokenDbContext(DbContextOptions<AuthenticationTokenDbContext> options, IOptions<EntityFrameworkCoreTokenStorageOptions> storageOptions) : DbContext(options)
{
    private readonly EntityFrameworkCoreTokenStorageOptions _storageOptions = storageOptions.Value;

    public DbSet<AuthenticationToken> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthenticationToken>().ToTable(_storageOptions.TableName);

        modelBuilder.Entity<AuthenticationToken>().Property(p => p.RefreshToken).HasColumnName(_storageOptions.RefreshTokenColumnName);
        modelBuilder.Entity<AuthenticationToken>().Property(p => p.RefreshTokenUpdatedAt).HasColumnName(_storageOptions.RefreshTokenUpdatedAtColumnName);

        modelBuilder.Entity<AuthenticationToken>().Property(p => p.AccessToken).HasColumnName(_storageOptions.AccessTokenColumnName);
        modelBuilder.Entity<AuthenticationToken>().Property(p => p.AccessTokenUpdatedAt).HasColumnName(_storageOptions.AccessTokenUpdatedAtColumnName);
        modelBuilder.Entity<AuthenticationToken>().Property(p => p.AccessTokenExpire).HasColumnName(_storageOptions.AccessTokenExpireColumnName);
    }
}