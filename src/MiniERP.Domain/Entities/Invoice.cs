using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Events;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Domain.Entities;

public sealed class Invoice : Entity
{
    public string InvoiceNo { get; private set; } = default!;
    public Guid OrderId { get; private set; }
    public Guid DealerId { get; private set; }
    public InvoiceType Type { get; private set; }
    public decimal Amount { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public DateOnly? IssuedDate { get; private set; }

    private Invoice() { }

    public static Invoice Create(string invoiceNo, Guid orderId, Guid dealerId, InvoiceType type, decimal amount)
    {
        if (amount <= 0) throw new DomainException("Amount phải > 0.");
        return new Invoice
        {
            InvoiceNo = invoiceNo,
            OrderId = orderId,
            DealerId = dealerId,
            Type = type,
            Amount = amount,
        };
    }

    public void Issue(DateOnly issuedDate)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"Không thể phát hành hóa đơn ở trạng thái {Status}.");
        Status = InvoiceStatus.Issued;
        IssuedDate = issuedDate;
        Touch();
        Raise(new InvoiceIssuedEvent(Id, OrderId));
    }

    public void Cancel()
    {
        if (Status != InvoiceStatus.Issued)
            throw new DomainException($"Không thể hủy hóa đơn ở trạng thái {Status}.");
        Status = InvoiceStatus.Cancelled;
        Touch();
    }
}
