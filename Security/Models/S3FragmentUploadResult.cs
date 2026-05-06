namespace ProyectoInnovador.Security.Models;

public sealed class S3FragmentUploadResult
{
    public required string FragmentKey { get; init; }
    public required string ETag { get; init; }
    public int FragmentIndex { get; init; }
    public int TotalFragments { get; init; }
    public int SizeBytes { get; init; }
}
