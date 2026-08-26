using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Events;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Domain.Entities;

public sealed class DealerContract : Entity
{
    public string ContractNo { get; private set; } = default!;
    public Guid DealerId { get; private set; }
    public Guid BankId { get; private set; }
    public decimal ContractValue { get; private set; }
    public ContractStatus Status { get; private set; } = ContractStatus.Draft;
    public DateTimeOffset? DealerSignedAt { get; private set; }
    public DateTimeOffset? ApprovedA1At { get; private set; }
    public DateTimeOffset? ApprovedA2At { get; private set; }

    private DealerContract() { }

    public static DealerContract Create(string contractNo, Guid dealerId, Guid bankId, decimal contractValue)
    {
        if (contractValue <= 0) throw new DomainException("ContractValue phải > 0.");
        return new DealerContract
        {
            ContractNo = contractNo,
            DealerId = dealerId,
            BankId = bankId,
            ContractValue = contractValue,
        };
    }

    public void DealerSign()
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Không thể ký hợp đồng ở trạng thái {Status}.");
        Status = ContractStatus.DealerSigned;
        DealerSignedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void ApproveA1()
    {
        if (Status != ContractStatus.DealerSigned)
            throw new DomainException($"Không thể duyệt A1 ở trạng thái {Status}.");
        Status = ContractStatus.ApprovedA1;
        ApprovedA1At = DateTimeOffset.UtcNow;
        Touch();
    }

    public void ApproveA2()
    {
        if (Status != ContractStatus.ApprovedA1)
            throw new DomainException($"Không thể duyệt A2 ở trạng thái {Status}.");
        Status = ContractStatus.ApprovedA2;
        ApprovedA2At = DateTimeOffset.UtcNow;
        Touch();
        Raise(new ContractApprovedEvent(Id, DealerId));
    }

    public void Terminate()
    {
        Status = ContractStatus.Terminated;
        Touch();
    }
}
