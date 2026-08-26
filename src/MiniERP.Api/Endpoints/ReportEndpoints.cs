using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Reports;

namespace MiniERP.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization();

        group.MapGet("/dealer-summary", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new DealerSummaryReportQuery(), ct)));

        group.MapGet("/guarantee-expiring", async (int? withinDays, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GuaranteeExpiringReportQuery(withinDays ?? 30), ct)));
    }
}
