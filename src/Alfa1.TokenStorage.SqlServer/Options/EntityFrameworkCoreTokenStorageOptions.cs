using System.ComponentModel.DataAnnotations;

namespace Alfa1.TokenStorage.SqlServer.Options;

public class EntityFrameworkCoreTokenStorageOptions
{
    [Required]
    public string ConnectionStringName { get; set; } = "Tokens";

    [Required]
    public string TableName { get; set; } = "Tokens";

    [Required]
    public string RefreshTokenColumnName { get; set; } = "RefreshToken";

    [Required]
    public string RefreshTokenUpdatedAtColumnName { get; set; } = "RefreshTokenUpdatedAt";

    [Required]
    public string AccessTokenColumnName { get; set; } = "AccessToken";

    [Required]
    public string AccessTokenUpdatedAtColumnName { get; set; } = "AccessTokenUpdatedAt";

    [Required]
    public string AccessTokenExpireColumnName { get; set; } = "AccessTokenExpireAt";
}