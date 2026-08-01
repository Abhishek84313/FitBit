using FitBit.Api.Dtos.Goals;
using FitBit.Api.Extensions;
using FitBit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBit.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public sealed class GoalsController : ControllerBase
{
    private readonly IGoalService _goals;

    public GoalsController(IGoalService goals) => _goals = goals;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoalResponse>>> List(
        [FromQuery] bool activeOnly = false, CancellationToken ct = default) =>
        Ok(await _goals.ListAsync(User.GetUserId(), activeOnly, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<GoalResponse>> Get(string id, CancellationToken ct) =>
        Ok(await _goals.GetAsync(id, User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<GoalResponse>> Create(GoalCreateRequest request, CancellationToken ct)
    {
        var created = await _goals.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GoalResponse>> Update(string id, GoalUpdateRequest request, CancellationToken ct) =>
        Ok(await _goals.UpdateAsync(id, User.GetUserId(), request, ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _goals.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
