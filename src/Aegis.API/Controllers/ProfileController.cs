using Aegis.Application.Profile.Commands;
using Aegis.Application.Profile.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

[Authorize]
public class ProfileController : BaseApiController
{
    /// <summary>Get the current user's profile.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetProfileQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Update the current user's profile.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var command = new UpdateProfileCommand(
            UserId: CurrentUserId,
            CurrentRole: request.CurrentRole,
            YearsExperience: request.YearsExperience,
            LocationCountry: request.LocationCountry,
            LocationCity: request.LocationCity,
            EnglishLevel: request.EnglishLevel,
            RemotePreference: request.RemotePreference,
            SalaryMin: request.SalaryMin,
            SalaryMax: request.SalaryMax,
            SalaryCurrency: request.SalaryCurrency,
            CareerGoals: request.CareerGoals);

        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Add or update a skill in the user's profile.</summary>
    [HttpPost("skills")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSkill(
        [FromBody] AddSkillRequest request,
        CancellationToken ct)
    {
        var command = new AddSkillCommand(
            UserId: CurrentUserId,
            SkillId: request.SkillId,
            SelfRatedLevel: request.SelfRatedLevel,
            YearsExperience: request.YearsExperience,
            IsPrimary: request.IsPrimary);

        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Remove a skill from the user's profile.</summary>
    [HttpDelete("skills/{skillId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSkill(int skillId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveSkillCommand(CurrentUserId, skillId), ct);
        return NoContent();
    }
}

public record UpdateProfileRequest(
    string? CurrentRole,
    int? YearsExperience,
    string? LocationCountry,
    string? LocationCity,
    string? EnglishLevel,
    string? RemotePreference,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? CareerGoals);

public record AddSkillRequest(
    int SkillId,
    int SelfRatedLevel,
    double? YearsExperience,
    bool IsPrimary);
