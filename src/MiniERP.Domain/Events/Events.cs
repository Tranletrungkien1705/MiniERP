using MiniERP.Domain.Common;

namespace MiniERP.Domain.Events;

public sealed record ContractApprovedEvent(Guid ContractId, Guid DealerId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record OrderApprovedEvent(Guid OrderId, Guid DealerId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record StockDeliveredEvent(Guid StockItemId, Guid OrderId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InvoiceIssuedEvent(Guid InvoiceId, Guid OrderId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
