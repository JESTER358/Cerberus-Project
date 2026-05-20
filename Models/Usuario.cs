namespace ProyectoInnovador.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free";       // Free | Premium | Enterprise
    public bool EsAdmin { get; set; } = false;
}
