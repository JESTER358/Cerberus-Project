namespace ProyectoInnovador.Security.Models;

public sealed class S3UploadBatchResult
{
    public required string BucketName { get; init; }
    public required string BaseFileName { get; init; }
    public required IReadOnlyList<S3FragmentUploadResult> Fragments { get; init; }
    public int TotalFragments => Fragments.Count;
}
