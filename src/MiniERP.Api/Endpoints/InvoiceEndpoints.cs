using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Invoices;
using MiniERP.Domain.Enums;

namespace MiniERP.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices").WithTags("Invoices").RequireAuthorization();

        group.MapGet("/by-dealer/{dealerId:guid}", async (Guid dealerId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetInvoicesByDealerQuery(dealerId), ct)));

        group.MapPost("/", async (CreateInvoiceCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/invoices/{result.Id}", result);
        }).RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/issue", async (Guid id, IssueBody body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new IssueInvoiceCommand(id, body.IssuedDate), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));
    }
}

file sealed record IssueBody(DateOnly IssuedDate);
