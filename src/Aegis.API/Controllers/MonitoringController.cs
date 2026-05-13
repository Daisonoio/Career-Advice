using Aegis.Application.Monitoring.Commands;
using Aegis.Application.Monitoring.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

[Authorize]
public class MonitoringController : BaseApiController
{
    /// <summary>Get the monitoring dashboard summary for the current user.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMonitoringDashboardQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Get paginated alerts for the current user.</summary>
    [HttpGet("alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetAlertsQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Mark an alert as read.</summary>
    [HttpPut("alerts/{alertId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAlertRead(int alertId, CancellationToken ct)
    {
        await Mediator.Send(new MarkAlertReadCommand(CurrentUserId, alertId), ct);
        return NoContent();
    }

    /// <summary>
    /// Get the competitiveness history for the current user (up to 12 weekly snapshots).
    /// </summary>
    [HttpGet("competitiveness-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCompetitivenessHistory(
        [FromQuery] int limit = 12,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetCompetitivenessHistoryQuery(CurrentUserId, limit), ct);
        return Ok(result);
    }
}
