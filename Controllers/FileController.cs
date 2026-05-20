using Microsoft.AspNetCore.Mvc;
using ProyectoInnovador.Security.Contracts;

namespace ProyectoInnovador.Controllers;

[ApiController]
[Route("files")]
public sealed class FileController : ControllerBase
{
    private readonly IFileOrchestrator _fileOrchestrator;

    public FileController(IFileOrchestrator fileOrchestrator)
    {
        _fileOrchestrator = fileOrchestrator;
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        try
        {
            // Endpoint API heredado — requiere la semilla vía header X-Seed
            var seed = Request.Headers["X-Seed"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(seed))
                return BadRequest("Header X-Seed requerido. Usa la semilla recibida al cifrar.");
            var fileBytes = await _fileOrchestrator.DownloadAndReassembleAsync(id, seed, HttpContext.RequestAborted);
            return File(fileBytes, "application/octet-stream", $"archivo_recuperado_{id}.bin");
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            return StatusCode(500, $"Error de Integridad o Seguridad: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
}
