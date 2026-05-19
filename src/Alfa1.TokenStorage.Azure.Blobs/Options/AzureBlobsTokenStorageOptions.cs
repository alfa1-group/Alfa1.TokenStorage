using System.ComponentModel.DataAnnotations;

namespace Alfa1.TokenStorage.Azure.Blobs.Options;

public class AzureBlobsTokenStorageOptions
{
    [Required]
    public string ConnectionString { get; set; } = null!;

    [Required]
    public string ContainerName { get; set; } = null!;

    [Required] 
    public string RefreshTokenFilePath { get; set; } = "refreshtoken.txt";

    [Required]
    public string AccessTokenFilePath { get; set; } = "accesstoken.txt";

    public string TokenIdentifier { get; set; } = string.Empty;
}