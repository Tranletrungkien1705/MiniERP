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

        // Import hàng loạt từ nguồn dữ liệu thật — không yêu cầu JWT, cùng convention với import/partners.
        app.MapPost("/api/import/warehouses", async (List<ImportWarehouseRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            return Results.Ok(await sender.Send(new ImportWarehousesCommand(rows), ct));
        }).WithTags("Inventory").AllowAnonymous();

        app.MapPost("/api/import/products", async (List<ImportProductRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            return Results.Ok(await sender.Send(new ImportProductsCommand(rows), ct));
        }).WithTags("Inventory").AllowAnonymous();

        app.MapPost("/api/import/stockitems", async (List<ImportStockItemRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            return Results.Ok(await sender.Send(new ImportStockItemsCommand(rows), ct));
        }).WithTags("Inventory").AllowAnonymous();
    }
}

file sealed record DeliverBody(Guid OrderId);
