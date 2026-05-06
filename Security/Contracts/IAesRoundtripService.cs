using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Contracts;

public interface IAesRoundtripService
{
    AesPayload Encrypt(byte[] plaintext, byte[] key, string keyVersion);
    byte[] Decrypt(AesPayload payload, byte[] key);
}
