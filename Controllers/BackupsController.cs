using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Authorize(Roles = "SuperAdmin"), Route("api/backups"), Tags("Backups")]
public sealed class BackupsController(IBackupService backups) : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        try { return Ok(new { items = backups.List() }); }
        catch (Exception) { return Problem("Backup storage is temporarily unavailable.", statusCode: 503); }
    }

    [HttpPost]
    public ActionResult Create()
    {
        try { return Accepted(backups.QueueInstant()); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception) { return Problem("Backup storage is temporarily unavailable.", statusCode: 503); }
    }

    [HttpGet("{id}/download")]
    public ActionResult Download(string id)
    {
        var path = backups.GetDownloadPath(id);
        if (path is null || !System.IO.File.Exists(path)) return NotFound(new { message = "Backup is unavailable or has expired." });
        return PhysicalFile(path, "application/gzip", Path.GetFileName(path), enableRangeProcessing: true);
    }
}
