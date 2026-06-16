using iAdmin.Common.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iAdmin.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UpdatesController : ControllerBase
{
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(ILogger<UpdatesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the latest available update information
    /// </summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(UpdateInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<UpdateInfoDto>> GetLatestUpdate()
    {
        try
        {
            // For now, return no update available
            // In production, this would check against update service
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest update");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving update information" });
        }
    }

    /// <summary>
    /// Downloads an update package by version
    /// </summary>
    [HttpGet("download/{version}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadUpdate(string version)
    {
        try
        {
            _logger.LogInformation("Download request for version {Version}", version);

            // In production, this would serve the actual package
            return NotFound(new { message = $"Update version {version} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while downloading the update" });
        }
    }
}
