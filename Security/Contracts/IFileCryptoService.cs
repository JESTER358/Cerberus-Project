using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Contracts;

public interface IFileCryptoService
{
    AesPayload EncryptFile(byte[] fileBytes);
    byte[] DecryptFile(AesPayload encryptedFile);
}
