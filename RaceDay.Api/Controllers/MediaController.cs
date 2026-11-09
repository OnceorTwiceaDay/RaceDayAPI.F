using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
public class MediaController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly BlobImageStore _store;
    public MediaController(RaceDayDbContext db, BlobImageStore store)
    {
        _db = db;
        _store = store;
    }

    [HttpPost("api/events/{id:int}/banner")]
    [RequireRole(Role.Organiser)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<MediaUploadResponse>> UploadBanner(int id, IFormFile file, CancellationToken token)
    {
        var ev = await _db.Events.FindAsync(new object[] { id }, token);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();
        if (!BlobImageStore.IsAccepted(file) || !await HasAllowedSignature(file, token))
            return BadRequest(new { message = "Upload a JPG, PNG or WebP image of at most 5 MB." });
        if (!_store.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Azure image storage is not configured. Ask the organiser to set AzureStorage:ConnectionString." });
        ev.BannerImageUrl = await _store.Upload(file, "banners", token);
        await _db.SaveChangesAsync(token);
        return Ok(new MediaUploadResponse { MediaUrl = ev.BannerImageUrl });
    }

    [HttpPost("api/users/me/photo")]
    [RequireRole]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<MediaUploadResponse>> UploadProfilePhoto(IFormFile file, CancellationToken token)
    {
        var user = await _db.Users.FindAsync(new object[] { CurrentUserId }, token);
        if (user == null) return NotFound();
        if (!BlobImageStore.IsAccepted(file) || !await HasAllowedSignature(file, token))
            return BadRequest(new { message = "Upload a JPG, PNG or WebP image of at most 5 MB." });
        if (!_store.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Azure image storage is not configured. Set AzureStorage:ConnectionString." });
        user.ProfileImageUrl = await _store.Upload(file, "profiles", token);
        await _db.SaveChangesAsync(token);
        return Ok(new MediaUploadResponse { MediaUrl = user.ProfileImageUrl });
    }

    [HttpGet("api/media/{kind}/{name}")]
    public async Task<IActionResult> Download(string kind, string name, CancellationToken token)
    {
        if (kind is not ("banners" or "profiles") ||
            !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-f0-9]{32}\.(jpg|png|webp)$"))
            return NotFound();
        // Only serve image keys referenced in existing records.
        var path = $"/api/media/{kind}/{name}";
        var linked = kind == "banners"
            ? await _db.Events.AnyAsync(e => e.BannerImageUrl == path, token)
            : await _db.Users.AnyAsync(u => u.ProfileImageUrl == path, token);
        if (!linked) return NotFound();
        if (!_store.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Azure image storage is not configured." });
        var media = await _store.Download(kind, name, token);
        return media == null ? NotFound() : File(media.Value.Stream, media.Value.ContentType);
    }

    private static async Task<bool> HasAllowedSignature(IFormFile file, CancellationToken token)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(), token);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.AsSpan(0,8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}),
            ".webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header,0,4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header,8,4) == "WEBP",
            _ => false
        };
    }
}
