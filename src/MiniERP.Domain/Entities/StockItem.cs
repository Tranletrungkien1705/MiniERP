using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;
using MiniERP.Domain.Events;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Domain.Entities;

public sealed class StockItem : Entity
{
    public string SerialNo { get; private set; } = default!; // VIN-like unique serial
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public StockItemStatus Status { get; private set; } = StockItemStatus.InStock;

    private readonly List<StockMovement> _movements = [];
    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    private StockItem() { }

    public static StockItem Receive(string serialNo, Guid productId, Guid warehouseId)
    {
        var item = new StockItem
        {
            SerialNo = serialNo,
            ProductId = productId,
            WarehouseId = warehouseId,
        };
        item._movements.Add(StockMovement.Create(item.Id, StockMovementType.ReceiveFromFactory, null, warehouseId));
        return item;
    }

    public void TransferTo(Guid warehouseId)
    {
        if (Status != StockItemStatus.InStock)
            throw new DomainException($"Không thể chuyển kho khi trạng thái là {Status}.");
        _movements.Add(StockMovement.Create(Id, StockMovementType.TransferWarehouse, WarehouseId, warehouseId));
        WarehouseId = warehouseId;
        Touch();
    }

    public void Reserve()
    {
        if (Status != StockItemStatus.InStock)
            throw new DomainException($"Không thể giữ chỗ khi trạng thái là {Status}.");
        Status = StockItemStatus.Reserved;
        Touch();
    }

    public void DeliverToDealer(Guid orderId)
    {
        if (Status != StockItemStatus.Reserved)
            throw new DomainException($"Không thể giao xe khi trạng thái là {Status}.");
        Status = StockItemStatus.Delivered;
        _movements.Add(StockMovement.Create(Id, StockMovementType.DeliverToDealer, WarehouseId, null, orderId));
        Touch();
        Raise(new StockDeliveredEvent(Id, orderId));
    }
}

public sealed class StockMovement : Entity
{
    public Guid StockItemId { get; private set; }
    public StockMovementType Type { get; private set; }
    public Guid? FromWarehouseId { get; private set; }
    public Guid? ToWarehouseId { get; private set; }
    public Guid? RefOrderId { get; private set; }

    private StockMovement() { }

    internal static StockMovement Create(Guid stockItemId, StockMovementType type, Guid? fromWarehouseId, Guid? toWarehouseId, Guid? refOrderId = null) => new()
    {
        StockItemId = stockItemId,
        Type = type,
        FromWarehouseId = fromWarehouseId,
        ToWarehouseId = toWarehouseId,
        RefOrderId = refOrderId,
    };
}
