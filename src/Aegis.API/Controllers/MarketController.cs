using Aegis.Application.Market.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

public class MarketController : BaseApiController
{
    /// <summary>Get market KPIs for a specific skill.</summary>
    [HttpGet("skills/{skillId:int}/kpis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkillKpis(
        int skillId,
        [FromQuery] string? geo = null,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetSkillKpisQuery(skillId, geo), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Get market KPIs for a specific role.</summary>
    [HttpGet("roles/{role}/kpis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleKpis(
        string role,
        [FromQuery] string? geo = null,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetRoleKpisQuery(role, geo), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Get market trends (emerging and declining skills).</summary>
    [HttpGet("trends")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrends(
        [FromQuery] string? geo = null,
        [FromQuery] int topN = 10,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetMarketTrendsQuery(geo, topN), ct);
        return Ok(result);
    }
}
