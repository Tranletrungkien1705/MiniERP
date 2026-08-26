using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Exceptions;
using Xunit;

namespace MiniERP.Domain.Tests;

public class SalesOrderTests
{
    [Fact]
    public void FullFlow_DemandToCompleted_Succeeds()
    {
        var order = SalesOrder.Create("SO-001", Guid.NewGuid(), Guid.NewGuid());
        var productId = Guid.NewGuid();

        order.AddDemand(productId, 10);
        var line = Assert.Single(order.Lines);

        order.Supply(line.Id, 8);
        Assert.Equal(OrderStatus.Supplied, order.Status);

        order.ApproveA1();
        order.ApproveA2();
        order.Complete();

        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Supply_MoreThanDemand_Throws()
    {
        var order = SalesOrder.Create("SO-002", Guid.NewGuid(), Guid.NewGuid());
        order.AddDemand(Guid.NewGuid(), 5);
        var line = order.Lines.Single();

        Assert.Throws<DomainException>(() => order.Supply(line.Id, 6));
    }

    [Fact]
    public void Cancel_AfterCompleted_Throws()
    {
        var order = SalesOrder.Create("SO-003", Guid.NewGuid(), Guid.NewGuid());
        order.AddDemand(Guid.NewGuid(), 1);
        var line = order.Lines.Single();
        order.Supply(line.Id, 1);
        order.ApproveA1();
        order.ApproveA2();
        order.Complete();

        Assert.Throws<DomainException>(() => order.Cancel());
    }
}
