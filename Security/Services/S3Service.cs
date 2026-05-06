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

            var config = new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = _options.ForcePathStyle
            };
            _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }

        //logica de Fragmentacion (Partir archivos)
        public async Task<S3UploadBatchResult> SubirArchivoFragmentadoAsync(string nombreBase, byte[] datosCompletos, int tamanoFragmentoBytes)
        {
            if (tamanoFragmentoBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tamanoFragmentoBytes), "Fragment size must be > 0.");
            }

            int totalFragmentos = (int)Math.Ceiling((double)datosCompletos.Length / tamanoFragmentoBytes);
            var resultados = new List<S3FragmentUploadResult>(capacity: totalFragmentos);

            await EnsureBucketExistsAsync();
            
            for (int i = 0; i < totalFragmentos; i++)
            {
                int offset = i * tamanoFragmentoBytes;
                int tamanoActual = Math.Min(tamanoFragmentoBytes, datosCompletos.Length - offset);
                
                byte[] fragmento = new byte[tamanoActual];
                Buffer.BlockCopy(datosCompletos, offset, fragmento, 0, tamanoActual);

                // formato: archivo.bin.part_001
                string nombreFragmento = $"{nombreBase}.part_{i + 1:D3}";
                
                Console.WriteLine($"[CERBERUS] Subiendo fragmento {i + 1}/{totalFragmentos}: {nombreFragmento}");
                await using var stream = new MemoryStream(fragmento, writable: false);
                var etag = await SubirFragmentoAsync(nombreFragmento, stream);
                resultados.Add(new S3FragmentUploadResult
                {
                    FragmentKey = nombreFragmento,
                    ETag = etag,
                    FragmentIndex = i + 1,
                    TotalFragments = totalFragmentos,
                    SizeBytes = tamanoActual
                });
            }

            return new S3UploadBatchResult
            {
                BucketName = _options.BucketName,
                BaseFileName = nombreBase,
                Fragments = resultados
            };
        }

        private async Task EnsureBucketExistsAsync()
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _options.BucketName);
            if (exists)
            {
                return;
            }

            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _options.BucketName
            });
        }

        public async Task<string> SubirFragmentoAsync(string nombreKey, Stream datos, CancellationToken cancellationToken = default)
        {
            if (datos is null)
            {
                throw new ArgumentNullException(nameof(datos));
            }

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
