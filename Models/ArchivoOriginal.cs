namespace ProyectoInnovador.Models;

public class ArchivoOriginal
{
    public int Id { get; set; }

    /// <summary>
    /// Identificador público expuesto en vistas y al usuario.
    /// GUID para evitar IDOR — el Id entero nunca sale de la capa de datos.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string Nombre { get; set; } = string.Empty;
    public long Tamano { get; set; }
    public string HashSha256 { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 de la semilla usada para cifrar.
    /// Se guarda para validar la semilla al descifrar sin exponer la semilla real.
    /// </summary>
    public string SeedHash { get; set; } = string.Empty;

    public ICollection<Fragmento> Fragmentos { get; set; } = new List<Fragmento>();
}
