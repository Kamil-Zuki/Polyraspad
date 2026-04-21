namespace MediaService.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string Bucket { get; set; } = "polyraspad-media";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public bool UsePathStyle { get; set; } = true;
    public string? PublicBaseUrl { get; set; }
    public string? ServerFetchBaseUrl { get; set; }
    public int PresignedUrlExpirationMinutes { get; set; } = 60;
}
