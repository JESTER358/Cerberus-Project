namespace ProyectoInnovador.Security.Models;

public sealed class S3StorageOptions
{
    public string ServiceUrl { get; set; } = "http://localhost:9000";
    public bool ForcePathStyle { get; set; } = true;
    public string BucketName { get; set; } = "cerberus-fragments";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool RunStartupDemoUpload { get; set; }
}
