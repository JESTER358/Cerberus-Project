namespace ProyectoInnovador.Security.Services;

using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using ProyectoInnovador.Security.Contracts;

/// <summary>
/// Implementación nativa de <see cref="IAzureService"/> sobre Azure Blob Storage.
/// Utiliza exclusivamente <see cref="Stream"/> para proteger la memoria RAM del servidor
/// ante archivos industriales de gran tamaño (+750 MB). Queda estrictamente prohibido
/// el uso de <c>byte[]</c> en cualquier operación de este servicio.
/// </summary>
public class AzureService : IAzureService
{
    private readonly BlobContainerClient _containerClient;

    /// <summary>
    /// Inicializa el cliente de Azure Blob Storage leyendo la configuración
    /// desde la sección <c>CloudProviderSettings:AzureBlobStorage</c>.
    /// </summary>
    /// <param name="configuration">Instancia de <see cref="IConfiguration"/> inyectada por el DI container.</param>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la cadena de conexión o el nombre del contenedor no están definidos en la configuración.
    /// </exception>
    public AzureService(IConfiguration configuration)
    {
        var section = configuration.GetSection("CloudProviderSettings:AzureBlobStorage");

        var connectionString = section["ConnectionString"]
            ?? throw new InvalidOperationException(
                "La clave 'CloudProviderSettings:AzureBlobStorage:ConnectionString' no está definida en appsettings.json.");

        var containerName = section["ContainerName"]
            ?? throw new InvalidOperationException(
                "La clave 'CloudProviderSettings:AzureBlobStorage:ContainerName' no está definida en appsettings.json.");

        // BlobServiceClient se inicializa una sola vez (Singleton) y es thread-safe.
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// El Stream se transmite directamente a Azure sin intermediarios en memoria.
    /// El blob se sobreescribe si ya existe (<c>overwrite: true</c>).
    /// </remarks>
    public async Task UploadFragmentAsync(
        string fragmentName,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentName);
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            // Garantiza que el contenedor existe sin fallar si ya fue creado.
            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            BlobClient blobClient = _containerClient.GetBlobClient(fragmentName);

            // UploadAsync transmite el stream directamente; nunca materializa byte[].
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            // Error estructurado de Azure: código HTTP + código de error del servicio.
            throw new InvalidOperationException(
                $"[AzureService] Upload falló para '{fragmentName}'. Status={ex.Status}, Code={ex.ErrorCode}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// El <see cref="Stream"/> retornado proviene directamente de la respuesta HTTP de Azure.
    /// El llamador DEBE disponer el stream en un bloque <c>using</c> para liberar la conexión.
    /// </remarks>
    public async Task<Stream> DownloadFragmentAsync(
        string fragmentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentName);

        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fragmentName);

            // DownloadStreamingAsync retorna el stream de la respuesta HTTP sin buffering.
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"[AzureService] Download falló para '{fragmentName}'. Status={ex.Status}, Code={ex.ErrorCode}: {ex.Message}", ex);
        }
    }
}
