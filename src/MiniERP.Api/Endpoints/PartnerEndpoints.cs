using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Partners;
using MiniERP.Domain.Enums;

namespace MiniERP.Api.Endpoints;

public static class PartnerEndpoints
{
    public static void MapPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/partners").WithTags("Partners").RequireAuthorization();

        group.MapGet("/", async (PartnerType? type, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetPartnersQuery(type), ct)));

        group.MapPost("/", async (CreatePartnerCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/partners/{result.Id}", result);
        }).RequireAuthorization(p => p.RequireRole(nameof(PartnerType.Principal)));

        // Import hàng loạt từ nguồn dữ liệu thật (Mst_Dealer...) — không yêu cầu JWT, cùng convention với các app _labs khác.
        app.MapPost("/api/import/partners", async (List<ImportPartnerRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            var result = await sender.Send(new ImportPartnersCommand(rows), ct);
            return Results.Ok(result);
        }).WithTags("Partners").AllowAnonymous();
    }
}
