using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Domain.Entities;

public sealed class Payment : Entity
{
    public Guid ContractId { get; private set; }
    public PaymentType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly PaidDate { get; private set; }
    public string? Note { get; private set; }

    private Payment() { }

    public static Payment Record(Guid contractId, PaymentType type, decimal amount, DateOnly paidDate, string? note = null)
    {
        if (amount <= 0) throw new DomainException("Amount phải > 0.");
        return new Payment
        {
            ContractId = contractId,
            Type = type,
            Amount = amount,
            PaidDate = paidDate,
            Note = note,
        };
    }
}
