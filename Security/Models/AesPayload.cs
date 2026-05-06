namespace ProyectoInnovador.Security.Models;

public sealed class AesPayload
{
    public byte[] Ciphertext { get; init; } = [];
    public byte[] Iv { get; init; } = [];
    public string KeyVersion { get; init; } = string.Empty;
}
