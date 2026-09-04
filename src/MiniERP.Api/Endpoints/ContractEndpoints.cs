using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Contracts;
using MiniERP.Domain.Enums;

namespace MiniERP.Api.Endpoints;

public static class ContractEndpoints
{
    public static void MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contracts").WithTags("Contracts").RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetContractByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (CreateContractCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/contracts/{result.Id}", result);
        }).RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/dealer-sign", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new DealerSignContractCommand(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Dealer)));

        group.MapPost("/{id:guid}/approve-a1", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ApproveContractA1Command(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        group.MapPost("/{id:guid}/approve-a2", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ApproveContractA2Command(id), ct)))
            .RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        // Import hàng loạt từ nguồn dữ liệu thật (Dlr_Contract) — không yêu cầu JWT, cùng convention import/partners.
        app.MapPost("/api/import/contracts", async (List<ImportContractRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            var result = await sender.Send(new ImportContractsCommand(rows), ct);
            return Results.Ok(result);
        }).WithTags("Contracts").AllowAnonymous();
    }
}
