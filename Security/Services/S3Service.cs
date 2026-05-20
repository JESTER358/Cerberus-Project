using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using ProyectoInnovador.Security.Models;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Net.Http;

namespace ProyectoInnovador.Security.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3StorageOptions _options;

        public S3Service(IOptions<S3StorageOptions> options)
        {
            _options = options.Value;
            if (string.IsNullOrWhiteSpace(_options.AccessKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException("S3 credentials are missing. Configure S3:AccessKey and S3:SecretKey.");
            }

            // ServiceURL y RegionEndpoint son mutuamente excluyentes en el SDK de AWS.
            // Para AWS real: usar RegionEndpoint y omitir ServiceURL.
            // Para MinIO local: usar ServiceURL + ForcePathStyle = true y omitir RegionEndpoint.
            AmazonS3Config config;
            if (string.IsNullOrWhiteSpace(_options.ServiceUrl) ||
                _options.ServiceUrl.Contains("amazonaws.com", StringComparison.OrdinalIgnoreCase))
            {
                // Modo AWS real — región explícita, sin ServiceURL
                config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.USEast2
                };
            }
            else
            {
                // Modo local (MinIO u otro emulador)
                config = new AmazonS3Config
                {
                    ServiceURL = _options.ServiceUrl,
                    ForcePathStyle = _options.ForcePathStyle
                };
            }
            _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }

        /// <summary>
        /// Sube un fragmento a AWS S3 de forma asíncrona.
        /// </summary>
        /// <param name="nombreKey">Nombre con el que se guardará el fragmento.</param>
        /// <param name="datos">Stream con los bytes del fragmento.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>El ETag asignado por S3 al objeto.</returns>
        public async Task<string> SubirFragmentoAsync(string nombreKey, Stream datos, CancellationToken cancellationToken = default)
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = nombreKey,
                InputStream = datos
            };

            try
            {
                var response = await _s3Client.PutObjectAsync(request, cancellationToken);
                return response.ETag;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Network error while uploading '{nombreKey}' to S3 endpoint '{_options.ServiceUrl}'.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException($"Upload timed out for '{nombreKey}'.", ex);
            }
        }

        /// <summary>
        /// Descarga un fragmento desde AWS S3 de forma asíncrona.
        /// </summary>
        /// <param name="nombreKey">Llave del objeto S3 a descargar.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Stream con el contenido del fragmento.</returns>
        public async Task<Stream> DescargarFragmentoAsync(string nombreKey, CancellationToken cancellationToken = default)
        {
            var request = new Amazon.S3.Model.GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = nombreKey
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
    }
}