using FitBit.Api.Dtos.Activities;
using FitBit.Api.Dtos.Common;
using FitBit.Api.Extensions;
using FitBit.Api.Models;
using FitBit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBit.Api.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activities;

    public ActivitiesController(IActivityService activities) => _activities = activities;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ActivityResponse>>> List(
        [FromQuery] string? type,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "date_desc",
        CancellationToken ct = default) =>
        Ok(await _activities.QueryAsync(User.GetUserId(), type, from, to, page, pageSize, sort, ct));

    /// <summary>Static list — a Distinct() over the collection would leave this empty on a fresh database.</summary>
    [HttpGet("types")]
    public ActionResult<string[]> Types() => Ok(ActivityTypes.All);

    [HttpGet("{id}")]
    public async Task<ActionResult<ActivityResponse>> Get(string id, CancellationToken ct) =>
        Ok(await _activities.GetAsync(id, User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<ActivityResponse>> Create(ActivityCreateRequest request, CancellationToken ct)
    {
        var created = await _activities.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ActivityResponse>> Update(
        string id, ActivityUpdateRequest request, CancellationToken ct) =>
        Ok(await _activities.UpdateAsync(id, User.GetUserId(), request, ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _activities.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
