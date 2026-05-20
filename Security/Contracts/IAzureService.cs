namespace ProyectoInnovador.Security.Contracts;

/// <summary>
/// Contrato para las operaciones de almacenamiento en Azure Blob Storage.
/// Todos los métodos operan exclusivamente con <see cref="Stream"/> para
/// proteger la memoria RAM del servidor ante archivos de gran tamaño (+750 MB).
/// </summary>
public interface IAzureService
{
    /// <summary>
    /// Sube un fragmento a Azure Blob Storage de forma asíncrona usando un Stream.
    /// </summary>
    /// <param name="fragmentName">Nombre del blob destino dentro del contenedor.</param>
    /// <param name="stream">Stream de origen. No se carga en memoria; se transmite directamente.</param>
    /// <param name="cancellationToken">Token de cancelación opcional.</param>
    Task UploadFragmentAsync(string fragmentName, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarga un fragmento desde Azure Blob Storage de forma asíncrona.
    /// </summary>
    /// <param name="fragmentName">Nombre del blob a recuperar.</param>
    /// <param name="cancellationToken">Token de cancelación opcional.</param>
    /// <returns>
    /// <see cref="Stream"/> con el contenido del blob.
    /// El llamador es responsable de disponer el stream una vez consumido.
    /// </returns>
    Task<Stream> DownloadFragmentAsync(string fragmentName, CancellationToken cancellationToken = default);
}
