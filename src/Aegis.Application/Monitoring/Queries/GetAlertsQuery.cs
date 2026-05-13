using Aegis.Application.Common.Models;
using Aegis.Application.Monitoring.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Monitoring.Queries;

public record GetAlertsQuery(int UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AlertResponse>>;

public class GetAlertsQueryHandler : IRequestHandler<GetAlertsQuery, PagedResult<AlertResponse>>
{
    private readonly IAlertRepository _alertRepository;

    public GetAlertsQueryHandler(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<PagedResult<AlertResponse>> Handle(GetAlertsQuery request, CancellationToken cancellationToken)
    {
        var alerts = await _alertRepository.GetByUserIdAsync(request.UserId, request.PageSize, request.Page, cancellationToken);
        var totalCount = await _alertRepository.GetTotalCountByUserIdAsync(request.UserId, cancellationToken);

        var items = alerts.Select(a => new AlertResponse(
            Id: a.Id,
            AlertType: a.AlertType.ToString(),
            Title: a.Title,
            Body: a.Body,
            Severity: a.Severity,
            CreatedAt: a.CreatedAt,
            IsRead: a.IsRead)).ToList();

        return new PagedResult<AlertResponse>(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize);
    }
}
