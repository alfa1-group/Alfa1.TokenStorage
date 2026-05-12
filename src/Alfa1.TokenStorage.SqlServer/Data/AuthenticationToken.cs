using System.ComponentModel.DataAnnotations;

namespace Alfa1.TokenStorage.SqlServer.Data;

public class AuthenticationToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1024)]
    public string RefreshToken { get; set; } = null!;

    public DateTimeOffset RefreshTokenUpdatedAt { get; set; } = DateTimeOffset.MinValue;

    [MaxLength(1024)]
    public string? AccessToken { get; set; }

    public DateTimeOffset AccessTokenUpdatedAt { get; set; } = DateTimeOffset.MinValue;

    public DateTimeOffset AccessTokenExpire { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Optimistic concurrency token.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}