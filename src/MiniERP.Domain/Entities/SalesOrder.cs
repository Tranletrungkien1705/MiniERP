using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Events;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Domain.Entities;

public sealed class SalesOrderLine : Entity
{
    public Guid SalesOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int DemandQty { get; private set; }
    public int SupplyQty { get; private set; }

    private SalesOrderLine() { }

    internal static SalesOrderLine Create(Guid salesOrderId, Guid productId, int demandQty) => new()
    {
        SalesOrderId = salesOrderId,
        ProductId = productId,
        DemandQty = demandQty,
    };

    internal void Supply(int supplyQty)
    {
        if (supplyQty < 0 || supplyQty > DemandQty)
            throw new DomainException("SupplyQty phải nằm trong khoảng [0, DemandQty].");
        SupplyQty = supplyQty;
    }
}

public sealed class SalesOrder : Entity
{
    public string OrderNo { get; private set; } = default!;
    public Guid DealerId { get; private set; }
    public Guid ContractId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Demand;

    private readonly List<SalesOrderLine> _lines = [];
    public IReadOnlyCollection<SalesOrderLine> Lines => _lines.AsReadOnly();

    private SalesOrder() { }

    public static SalesOrder Create(string orderNo, Guid dealerId, Guid contractId) => new()
    {
        OrderNo = orderNo,
        DealerId = dealerId,
        ContractId = contractId,
    };

    public void AddDemand(Guid productId, int demandQty)
    {
        if (Status != OrderStatus.Demand)
            throw new DomainException($"Không thể thêm nhu cầu ở trạng thái {Status}.");
        if (demandQty <= 0) throw new DomainException("DemandQty phải > 0.");
        _lines.Add(SalesOrderLine.Create(Id, productId, demandQty));
        Touch();
    }

    public void Supply(Guid lineId, int supplyQty)
    {
        if (Status != OrderStatus.Demand)
            throw new DomainException($"Không thể phân bổ hàng ở trạng thái {Status}.");
        var line = _lines.SingleOrDefault(l => l.Id == lineId)
            ?? throw new DomainException("Không tìm thấy dòng đơn hàng.");
        line.Supply(supplyQty);
        Status = OrderStatus.Supplied;
        Touch();
    }

    public void ApproveA1()
    {
        if (Status != OrderStatus.Supplied)
            throw new DomainException($"Không thể duyệt A1 ở trạng thái {Status}.");
        Status = OrderStatus.ApprovedA1;
        Touch();
    }

    public void ApproveA2()
    {
        if (Status != OrderStatus.ApprovedA1)
            throw new DomainException($"Không thể duyệt A2 ở trạng thái {Status}.");
        Status = OrderStatus.ApprovedA2;
        Touch();
        Raise(new OrderApprovedEvent(Id, DealerId));
    }

    public void Complete()
    {
        if (Status != OrderStatus.ApprovedA2)
            throw new DomainException($"Không thể hoàn tất ở trạng thái {Status}.");
        Status = OrderStatus.Completed;
        Touch();
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Completed)
            throw new DomainException("Đơn hàng đã hoàn tất, không thể hủy.");
        Status = OrderStatus.Cancelled;
        Touch();
    }
}
