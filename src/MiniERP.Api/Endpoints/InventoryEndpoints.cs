using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Inventory;
using MiniERP.Domain.Enums;

namespace MiniERP.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory").WithTags("Inventory").RequireAuthorization();

        group.MapGet("/by-warehouse/{warehouseId:guid}", async (Guid warehouseId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetStockByWarehouseQuery(warehouseId), ct)));

        group.MapPost("/receive", async (ReceiveStockCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/inventory/{result.Id}", result);
        }).RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/reserve", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ReserveStockCommand(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/deliver", async (Guid id, DeliverBody body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new DeliverStockCommand(id, body.OrderId), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));
    }
}

file sealed record DeliverBody(Guid OrderId);
