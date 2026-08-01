using FitBit.Api.Dtos.Dashboard;
using FitBit.Api.Extensions;
using FitBit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBit.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardResponse>> Summary(
        [FromQuery] int days = 7, CancellationToken ct = default) =>
        Ok(await _dashboard.GetSummaryAsync(User.GetUserId(), days, ct));
}
