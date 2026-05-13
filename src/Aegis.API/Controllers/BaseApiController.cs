using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aegis.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IMediator Mediator => HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected int CurrentUserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token."));
}
