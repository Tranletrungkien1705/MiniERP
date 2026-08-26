using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Reports;

public sealed record DealerSummaryRow(Guid DealerId, int OrderCount, int CompletedOrderCount, decimal InvoicedAmount);
public sealed record DealerSummaryReportQuery : IQuery<IReadOnlyList<DealerSummaryRow>>;

public sealed class DealerSummaryReportHandler(IAppDbContext db) : IQueryHandler<DealerSummaryReportQuery, IReadOnlyList<DealerSummaryRow>>
{
    public async Task<IReadOnlyList<DealerSummaryRow>> Handle(DealerSummaryReportQuery query, CancellationToken ct)
    {
        var orders = await db.Orders.AsNoTracking().ToListAsync(ct);
        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued)
            .ToListAsync(ct);

        return [.. orders
            .GroupBy(o => o.DealerId)
            .Select(g => new DealerSummaryRow(
                g.Key,
                g.Count(),
                g.Count(o => o.Status == OrderStatus.Completed),
                invoices.Where(i => i.DealerId == g.Key).Sum(i => i.Amount)))];
    }
}

public sealed record GuaranteeExpiringRow(Guid GuaranteeId, Guid ContractId, decimal Amount, DateOnly ExpiryDate);
public sealed record GuaranteeExpiringReportQuery(int WithinDays = 30) : IQuery<IReadOnlyList<GuaranteeExpiringRow>>;

public sealed class GuaranteeExpiringReportHandler(IAppDbContext db) : IQueryHandler<GuaranteeExpiringReportQuery, IReadOnlyList<GuaranteeExpiringRow>>
{
    public async Task<IReadOnlyList<GuaranteeExpiringRow>> Handle(GuaranteeExpiringReportQuery query, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var guarantees = await db.Guarantees.AsNoTracking()
            .Where(g => g.Status == GuaranteeStatus.Active)
            .ToListAsync(ct);

        return [.. guarantees
            .Where(g => g.IsExpiringSoon(today, query.WithinDays))
            .Select(g => new GuaranteeExpiringRow(g.Id, g.ContractId, g.Amount, g.ExpiryDate))];
    }
}
