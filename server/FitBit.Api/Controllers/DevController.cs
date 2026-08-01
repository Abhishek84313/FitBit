using FitBit.Api.Data;
using FitBit.Api.Extensions;
using FitBit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FitBit.Api.Controllers;

/// <summary>
/// Development-only. Program.cs registers a controller convention that removes
/// this type entirely outside Development, so it cannot ship by accident.
/// </summary>
[ApiController]
[Route("api/dev")]
public sealed class DevController : ControllerBase
{
    private readonly FitnessTrackerContext _context;
    private readonly ISeedService _seed;

    public DevController(FitnessTrackerContext context, ISeedService seed)
    {
        _context = context;
        _seed = seed;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        try
        {
            await _context.PingAsync(ct);

            var count = await _context.GetCollection<BsonDocument>()
                .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: ct);

            return Ok(new
            {
                mongo = "ok",
                database = _context.DatabaseName,
                collection = _context.CollectionName,
                documentCount = count,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mongo = "unreachable",
                database = _context.DatabaseName,
                collection = _context.CollectionName,
                error = ex.Message,
            });
        }
    }

    /// <summary>Without a shell installed, this is how the partial unique index gets confirmed.</summary>
    [HttpGet("indexes")]
    [AllowAnonymous]
    public async Task<IActionResult> Indexes(CancellationToken ct)
    {
        using var cursor = await _context.GetCollection<BsonDocument>().Indexes.ListAsync(ct);
        var indexes = await cursor.ToListAsync(ct);
        return Ok(indexes.Select(i => i.ToString()).ToList());
    }

    [HttpPost("seed")]
    [Authorize]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        var (activities, goals) = await _seed.SeedAsync(User.GetUserId(), ct);
        return Ok(new { seeded = new { activities, goals } });
    }

    [HttpDelete("reset")]
    [Authorize]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        var (activities, goals) = await _seed.ResetAsync(User.GetUserId(), ct);
        return Ok(new { deleted = new { activities, goals } });
    }
}
