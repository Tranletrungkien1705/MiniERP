using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Domain.Entities;

public sealed class Guarantee : Entity
{
    public Guid ContractId { get; private set; }
    public Guid BankId { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public GuaranteeStatus Status { get; private set; } = GuaranteeStatus.Active;

    private Guarantee() { }

    public static Guarantee Issue(Guid contractId, Guid bankId, decimal amount, DateOnly issueDate, DateOnly expiryDate)
    {
        if (expiryDate <= issueDate) throw new DomainException("ExpiryDate phải sau IssueDate.");
        return new Guarantee
        {
            ContractId = contractId,
            BankId = bankId,
            Amount = amount,
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
        };
    }

    public bool IsExpiringSoon(DateOnly asOf, int withinDays = 30) =>
        Status == GuaranteeStatus.Active && ExpiryDate.DayNumber - asOf.DayNumber <= withinDays;

    public void Clear()
    {
        if (Status != GuaranteeStatus.Active)
            throw new DomainException($"Không thể tất toán bảo lãnh ở trạng thái {Status}.");
        Status = GuaranteeStatus.Cleared;
        Touch();
    }

    public void MarkExpired()
    {
        if (Status == GuaranteeStatus.Active)
        {
            Status = GuaranteeStatus.Expired;
            Touch();
        }
    }
}
