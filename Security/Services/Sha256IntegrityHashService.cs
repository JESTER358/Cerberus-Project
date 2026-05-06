using System.Security.Cryptography;
using ProyectoInnovador.Security.Contracts;

namespace ProyectoInnovador.Security.Services;

public sealed class Sha256IntegrityHashService : IIntegrityHashService
{
    public string ComputeSha256(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }

    public bool AreEqual(string leftHash, string rightHash)
    {
        if (string.IsNullOrWhiteSpace(leftHash) || string.IsNullOrWhiteSpace(rightHash))
        {
            return false;
        }

        return string.Equals(leftHash, rightHash, StringComparison.OrdinalIgnoreCase);
    }
}
