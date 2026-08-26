using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Invoices;

public sealed record InvoiceDto(Guid Id, string InvoiceNo, Guid OrderId, Guid DealerId, InvoiceType Type, decimal Amount, InvoiceStatus Status, DateOnly? IssuedDate);

file static class Mapper
{
    public static InvoiceDto ToDto(Invoice i) => new(i.Id, i.InvoiceNo, i.OrderId, i.DealerId, i.Type, i.Amount, i.Status, i.IssuedDate);
}

public sealed record CreateInvoiceCommand(string InvoiceNo, Guid OrderId, Guid DealerId, InvoiceType Type, decimal Amount) : ICommand<InvoiceDto>;

public sealed class CreateInvoiceHandler(IAppDbContext db) : ICommandHandler<CreateInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        var invoice = Invoice.Create(command.InvoiceNo, command.OrderId, command.DealerId, command.Type, command.Amount);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(invoice);
    }
}

public sealed record IssueInvoiceCommand(Guid InvoiceId, DateOnly IssuedDate) : ICommand<InvoiceDto>;

public sealed class IssueInvoiceHandler(IAppDbContext db) : ICommandHandler<IssueInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(IssueInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstAsync(i => i.Id == command.InvoiceId, ct);
        invoice.Issue(command.IssuedDate);
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(invoice);
    }
}

public sealed record GetInvoicesByDealerQuery(Guid DealerId) : IQuery<IReadOnlyList<InvoiceDto>>;

public sealed class GetInvoicesByDealerHandler(IAppDbContext db) : IQueryHandler<GetInvoicesByDealerQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(GetInvoicesByDealerQuery query, CancellationToken ct)
    {
        return await db.Invoices.AsNoTracking()
            .Where(i => i.DealerId == query.DealerId)
            .Select(i => new InvoiceDto(i.Id, i.InvoiceNo, i.OrderId, i.DealerId, i.Type, i.Amount, i.Status, i.IssuedDate))
            .ToListAsync(ct);
    }
}
