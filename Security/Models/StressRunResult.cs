namespace ProyectoInnovador.Security.Models;

public sealed class StressRunResult
{
    public int SizeBytes { get; init; }
    public int Iteration { get; init; }
    public bool RoundtripPass { get; init; }
    public bool IntegrityPass { get; init; }
    public long ElapsedMilliseconds { get; init; }
}
