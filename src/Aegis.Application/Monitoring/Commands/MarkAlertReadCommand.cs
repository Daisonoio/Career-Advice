using Aegis.Application.Common.Exceptions;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Monitoring.Commands;

public record MarkAlertReadCommand(int UserId, int AlertId) : IRequest;

public class MarkAlertReadCommandHandler : IRequestHandler<MarkAlertReadCommand>
{
    private readonly IAlertRepository _alertRepository;

    public MarkAlertReadCommandHandler(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task Handle(MarkAlertReadCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAlert), request.AlertId);

        if (alert.UserId != request.UserId)
            throw new UnauthorizedException("This alert does not belong to the current user.");

        alert.MarkAsRead();
        await _alertRepository.MarkAsReadAsync(request.AlertId, cancellationToken);
    }
}
