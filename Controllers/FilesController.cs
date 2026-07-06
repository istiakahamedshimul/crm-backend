using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public class UploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string Category { get; set; } = "proofs";
}

[ApiController]
[Authorize]
[Route("api/files")]
[Tags("Files")]
public class FilesController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf", ".mp3", ".m4a", ".wav"
    };

    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Upload([FromForm] UploadRequest request)
    {
        var file = request.File;
        var category = request.Category;

        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Unsupported file type." });
        }

        var safeCategory = string.Concat(category.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(safeCategory)) safeCategory = "proofs";

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", safeCategory);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{safeCategory}/{fileName}";
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
        return Ok(new { fileName, url = absoluteUrl, relativeUrl });
    }
}
