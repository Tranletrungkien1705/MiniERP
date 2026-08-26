using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Exceptions;
using Xunit;

namespace MiniERP.Domain.Tests;

public class DealerContractTests
{
    [Fact]
    public void FullApprovalFlow_Succeeds_InOrder()
    {
        var contract = DealerContract.Create("CT-001", Guid.NewGuid(), Guid.NewGuid(), 1_000_000_000m);

        contract.DealerSign();
        contract.ApproveA1();
        contract.ApproveA2();

        Assert.Equal(ContractStatus.ApprovedA2, contract.Status);
        Assert.Single(contract.DomainEvents);
    }

    [Fact]
    public void ApproveA1_BeforeDealerSign_Throws()
    {
        var contract = DealerContract.Create("CT-002", Guid.NewGuid(), Guid.NewGuid(), 1_000_000_000m);

        Assert.Throws<DomainException>(() => contract.ApproveA1());
    }

    [Fact]
    public void Create_WithNonPositiveValue_Throws()
    {
        Assert.Throws<DomainException>(() => DealerContract.Create("CT-003", Guid.NewGuid(), Guid.NewGuid(), 0m));
    }
}
