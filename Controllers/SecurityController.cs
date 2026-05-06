using Microsoft.AspNetCore.Mvc;
using ProyectoInnovador.Security.Contracts;

namespace ProyectoInnovador.Controllers;

[ApiController]
[Route("security")]
public sealed class SecurityController : ControllerBase
{
    private readonly ISecurityCheckOrchestrator _orchestrator;

    public SecurityController(
        ISecurityCheckOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("demo-roundtrip")]
    public IActionResult DemoRoundtrip([FromBody] DemoRoundtripRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.PlaintextBase64))
        {
            return BadRequest("plaintext is required");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.PlaintextBase64);
        }
        catch (FormatException)
        {
            return BadRequest("plaintext must be base64");
        }

        var result = _orchestrator.RunFileRoundtrip(bytes);
        return Ok(new
        {
            keyVersion = result.Encrypted.KeyVersion,
            ivBase64 = Convert.ToBase64String(result.Encrypted.Iv),
            integrityPass = result.IntegrityPass,
            decryptedBase64 = Convert.ToBase64String(result.DecryptedBytes)
        });
    }

    [HttpPost("upload-multicloud")]
    public async Task<IActionResult> UploadMultiCloud([FromForm] IFormFile archivo)
    {
        try
        {
            if (archivo is null)
            {
                return BadRequest("archivo es requerido");
            }

            var result = await _orchestrator.UploadMultiCloudAsync(archivo, HttpContext.RequestAborted);

            return Ok(new
            {
                archivoId = result.ArchivoOriginal.Id,
                nombre = result.ArchivoOriginal.Nombre,
                tamano = result.ArchivoOriginal.Tamano,
                hashSha256 = result.ArchivoOriginal.HashSha256,
                s3ETag = result.S3ETag,
                fragmentos = result.ArchivoOriginal.Fragmentos.Select(fragmento => new
                {
                    proveedor = fragmento.CloudProvider,
                    url = fragmento.UrlRemota,
                    hash = fragmento.HashFragmento
                })
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public sealed class DemoRoundtripRequest
    {
        public string PlaintextBase64 { get; set; } = string.Empty;
    }
}
