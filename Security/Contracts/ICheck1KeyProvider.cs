namespace ProyectoInnovador.Security.Contracts;

public interface ICheck1KeyProvider
{
    byte[] GetRequiredKey();
    string GetActiveVersion();
}
