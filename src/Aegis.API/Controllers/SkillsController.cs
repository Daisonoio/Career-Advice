using Aegis.Application.Skills.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

public class SkillsController : BaseApiController
{
    /// <summary>Search skills by name.</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchSkills(
        [FromQuery] string q,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new SearchSkillsQuery(q, limit), ct);
        return Ok(result);
    }

    /// <summary>Get all skill categories with skill counts.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSkillCategoriesQuery(), ct);
        return Ok(result);
    }
}
