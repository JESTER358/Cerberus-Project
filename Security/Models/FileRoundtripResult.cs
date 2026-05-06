namespace ProyectoInnovador.Security.Models;

public sealed class FileRoundtripResult
{
    public required AesPayload Encrypted { get; init; }
    public required byte[] DecryptedBytes { get; init; }
    public required string OriginalHash { get; init; }
    public required string DecryptedHash { get; init; }
    public bool IntegrityPass { get; init; }
}
