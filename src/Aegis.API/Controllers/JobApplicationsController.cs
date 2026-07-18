using Aegis.Application.JobApplications.Commands;
using Aegis.Application.JobApplications.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

[Authorize]
public class JobApplicationsController : BaseApiController
{
    /// <summary>Submit a job posting: extracts required skills and compares them against the user's profile.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Submit([FromBody] SubmitJobApplicationRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SubmitJobApplicationCommand(
            CurrentUserId, request.JobTitle, request.CompanyName, request.RawDescription), ct);
        return Ok(result);
    }

    /// <summary>List the job applications submitted by the current user.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetJobApplicationHistoryQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Get a job application's detail: required skills, match score, and (once scored) the skill gap / training path.</summary>
    [HttpGet("{jobApplicationId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int jobApplicationId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetJobApplicationDetailQuery(CurrentUserId, jobApplicationId), ct);
        return Ok(result);
    }

    /// <summary>Start a custom test tailored to this job application's missing skills.</summary>
    [HttpPost("{jobApplicationId:int}/start-assessment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartAssessment(int jobApplicationId, CancellationToken ct)
    {
        var result = await Mediator.Send(new StartJobApplicationAssessmentCommand(CurrentUserId, jobApplicationId), ct);
        return Ok(result);
    }
}

public record SubmitJobApplicationRequest(string JobTitle, string? CompanyName, string RawDescription);
