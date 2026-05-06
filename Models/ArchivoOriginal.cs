namespace ProyectoInnovador.Models;

public class ArchivoOriginal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public long Tamano { get; set; }
    public string HashSha256 { get; set; } = string.Empty;

    public ICollection<Fragmento> Fragmentos { get; set; } = new List<Fragmento>();
}
