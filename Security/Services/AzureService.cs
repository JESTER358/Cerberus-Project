namespace ProyectoInnovador.Security.Services;
using Azure.Storage.Blobs;
using Azure;
using System;
using System.IO;
using System.Threading.Tasks;
public class AzureService
{
    //llave maestra para servidor local
    private readonly string connectionString = "UseDevelopmentStorage=true;";
    private readonly string containerName = "cerberus-azure-fragments";

    public async Task SubirFragmentoAzureAsync(string nombreArchivo, Stream fragmentoStream, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[INFO] Conectando con Azurite para enviar: {nombreArchivo}...");

            //instanciar el cliente forzando una version de API compatible con Azurite
            var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString, options);

            //conectar al contenedor (y crearlo si no existe)
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            //preparar el archivo
            BlobClient blobClient = containerClient.GetBlobClient(nombreArchivo);

            await blobClient.UploadAsync(fragmentoStream, overwrite: true, cancellationToken: cancellationToken);

            Console.WriteLine($"[EXITO] fragmento {nombreArchivo} asegurado en la vault de Azure");
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[ERROR AZURITE] fallo la subida (Status={ex.Status}, Code={ex.ErrorCode}): {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR AZURITE] fallo la subida: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Descarga un fragmento desde Azure Blob Storage de forma asíncrona.
    /// </summary>
    /// <param name="nombreArchivo">Nombre del blob a descargar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Stream con el contenido del fragmento.</returns>
    public async Task<Stream> DescargarFragmentoAzureAsync(string nombreArchivo, CancellationToken cancellationToken = default)
    {
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);
        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString, options);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(nombreArchivo);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
