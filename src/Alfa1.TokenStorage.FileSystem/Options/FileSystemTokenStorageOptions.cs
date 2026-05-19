using System.ComponentModel.DataAnnotations;

namespace Alfa1.TokenStorage.FileSystem.Options;

public class FileSystemTokenStorageOptions
{
    private const string DefaultTokenIdentifierPlaceholder = "{tokenIdentifier}";

    [Required]
    public string RefreshTokenFilePath { get; set; } = $"refreshtoken.{DefaultTokenIdentifierPlaceholder}.txt";

    [Required]
    public string AccessTokenFilePath { get; set; } = $"accesstoken.{DefaultTokenIdentifierPlaceholder}.txt";

    public string? RefreshTokenFilePathTemplate { get; set; }

    public string? AccessTokenFilePathTemplate { get; set; }

    public string TokenIdentifier { get; set; } = string.Empty;
}