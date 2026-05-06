namespace ProyectoInnovador.Security.Contracts;

public interface IIntegrityHashService
{
    string ComputeSha256(byte[] data);
    bool AreEqual(string leftHash, string rightHash);
}
