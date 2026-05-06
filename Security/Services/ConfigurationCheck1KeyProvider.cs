using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Exceptions;

namespace ProyectoInnovador.Security.Services;

public sealed class ConfigurationCheck1KeyProvider : ICheck1KeyProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationCheck1KeyProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public byte[] GetRequiredKey()
    {
        var keyBase64 = _configuration["Security:Check1KeyBase64"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new Check1KeyException("Missing configuration: Security:Check1KeyBase64");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException)
        {
            throw new Check1KeyException("Security:Check1KeyBase64 is not valid Base64.");
        }

        if (key.Length != 32)
        {
            throw new Check1KeyException("Security:Check1KeyBase64 must decode to exactly 32 bytes.");
        }

        return key;
    }

    public string GetActiveVersion()
    {
        var version = _configuration["Security:Check1KeyVersion"];
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new Check1KeyException("Missing configuration: Security:Check1KeyVersion");
        }

        return version;
    }
}
