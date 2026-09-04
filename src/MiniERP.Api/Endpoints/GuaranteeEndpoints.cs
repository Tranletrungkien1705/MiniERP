using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Guarantees;
using MiniERP.Application.Features.Payments;

namespace MiniERP.Api.Endpoints;

public static class GuaranteeEndpoints
{
    public static void MapGuaranteeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/import/guarantees", async (List<ImportGuaranteeRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            return Results.Ok(await sender.Send(new ImportGuaranteesCommand(rows), ct));
        }).WithTags("Guarantees").AllowAnonymous();

        app.MapPost("/api/import/payments", async (List<ImportPaymentRow> rows, ISender sender, CancellationToken ct) =>
        {
            if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
            return Results.Ok(await sender.Send(new ImportPaymentsCommand(rows), ct));
        }).WithTags("Payments").AllowAnonymous();
    }
}
