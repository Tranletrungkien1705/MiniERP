namespace MiniERP.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
