using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Orders;
using MiniERP.Domain.Enums;

namespace MiniERP.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/by-dealer/{dealerId:guid}", async (Guid dealerId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetOrdersByDealerQuery(dealerId), ct)));

        group.MapPost("/", async (CreateOrderCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/orders/{result.Id}", result);
        }).RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Dealer), nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/demand", async (Guid id, AddDemandBody body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new AddDemandLineCommand(id, body.ProductId, body.DemandQty), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Dealer)));

        group.MapPost("/{id:guid}/lines/{lineId:guid}/supply", async (Guid id, Guid lineId, SupplyBody body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new SupplyLineCommand(id, lineId, body.SupplyQty), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/approve-a1", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ApproveOrderA1Command(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/approve-a2", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ApproveOrderA2Command(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new CompleteOrderCommand(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        app.MapPost("/api/import/orders", async (List<ImportOrderRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            return Results.Ok(await sender.Send(new ImportOrdersCommand(rows), ct));
        }).WithTags("Orders").AllowAnonymous();
    }
}

file sealed record AddDemandBody(Guid ProductId, int DemandQty);
file sealed record SupplyBody(int SupplyQty);
