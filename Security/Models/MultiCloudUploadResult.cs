using ProyectoInnovador.Models;

namespace ProyectoInnovador.Security.Models;

public sealed class MultiCloudUploadResult
{
    public ArchivoOriginal ArchivoOriginal { get; set; } = new();
    public string S3ETag { get; set; } = string.Empty;
}
