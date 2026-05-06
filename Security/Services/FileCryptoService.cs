using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Services;

public sealed class FileCryptoService : IFileCryptoService
{
    private readonly IAesRoundtripService _aesRoundtripService;
    private readonly ICheck1KeyProvider _keyProvider;

    public FileCryptoService(IAesRoundtripService aesRoundtripService, ICheck1KeyProvider keyProvider)
    {
        _aesRoundtripService = aesRoundtripService;
        _keyProvider = keyProvider;
    }

    public AesPayload EncryptFile(byte[] fileBytes)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);
        var key = _keyProvider.GetRequiredKey();
        var version = _keyProvider.GetActiveVersion();

        return _aesRoundtripService.Encrypt(fileBytes, key, version);
    }

    public byte[] DecryptFile(AesPayload encryptedFile)
    {
        ArgumentNullException.ThrowIfNull(encryptedFile);
        var key = _keyProvider.GetRequiredKey();
        return _aesRoundtripService.Decrypt(encryptedFile, key);
    }
}
