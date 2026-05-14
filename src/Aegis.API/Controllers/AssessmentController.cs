using Aegis.Application.Assessment.Commands;
using Aegis.Application.Assessment.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.API.Controllers;

[Authorize]
public class AssessmentController : BaseApiController
{
    /// <summary>Start a new adaptive assessment.</summary>
    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> StartAssessment(CancellationToken ct)
    {
        var result = await Mediator.Send(new StartAssessmentCommand(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Submit an answer to the current assessment question.</summary>
    [HttpPost("{assessmentId:int}/answer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitAnswer(
        int assessmentId,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken ct)
    {
        var command = new SubmitAnswerCommand(
            UserId: CurrentUserId,
            AssessmentId: assessmentId,
            QuestionId: request.QuestionId,
            Answer: request.Answer);

        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Get the result of a completed assessment.</summary>
    [HttpGet("{assessmentId:int}/result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetResult(int assessmentId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAssessmentResultQuery(CurrentUserId, assessmentId), ct);
        return Ok(result);
    }

    /// <summary>Get the assessment history for the current user.</summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAssessmentHistoryQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Abandon an active assessment, unblocking a future start.</summary>
    [HttpDelete("{assessmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Abandon(int assessmentId, CancellationToken ct)
    {
        await Mediator.Send(new AbandonAssessmentCommand(CurrentUserId, assessmentId), ct);
        return NoContent();
    }
}

public record SubmitAnswerRequest(int QuestionId, string Answer);
