namespace ProyectoInnovador.Models;

public class Fragmento
{
    public int Id { get; set; }
    public string HashFragmento { get; set; } = string.Empty;
    public string CloudProvider { get; set; } = string.Empty;
    public string UrlRemota { get; set; } = string.Empty;

    public int ArchivoOriginalId { get; set; }
    public ArchivoOriginal? ArchivoOriginal { get; set; }
}
