using System.ComponentModel.DataAnnotations;

namespace Alfa1.TokenStorage.FileSystem.Options;

public class FileSystemTokenStorageOptions
{
    [Required]
    public string RefreshTokenFilePath { get; set; } = null!;

    [Required]
    public string AccessTokenFilePath { get; set; } = null!;

    public string TokenIdentifier { get; set; } = string.Empty;
}