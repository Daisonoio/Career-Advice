using Aegis.Application.Recommendations.Commands;
using Aegis.Application.Recommendations.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

[Authorize]
public class RecommendationsController : BaseApiController
{
    /// <summary>Generate a new career recommendation for the current user.</summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(CancellationToken ct)
    {
        var result = await Mediator.Send(new GenerateRecommendationCommand(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Get the latest career recommendation for the current user.</summary>
    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLatestRecommendationQuery(CurrentUserId), ct);
        if (result is null) return NotFound(new { message = "No recommendations found for this user." });
        return Ok(result);
    }

    /// <summary>Get the recommendation history for the current user (newest first).</summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetRecommendationHistoryQuery(CurrentUserId, limit), ct);
        return Ok(result);
    }
}
