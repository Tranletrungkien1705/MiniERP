using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Inventory;

public sealed record StockItemDto(Guid Id, string SerialNo, Guid ProductId, Guid WarehouseId, StockItemStatus Status);

file static class Mapper
{
    public static StockItemDto ToDto(StockItem s) => new(s.Id, s.SerialNo, s.ProductId, s.WarehouseId, s.Status);
}

public sealed record ReceiveStockCommand(string SerialNo, Guid ProductId, Guid WarehouseId) : ICommand<StockItemDto>;

public sealed class ReceiveStockHandler(IAppDbContext db) : ICommandHandler<ReceiveStockCommand, StockItemDto>
{
    public async Task<StockItemDto> Handle(ReceiveStockCommand command, CancellationToken ct)
    {
        var item = StockItem.Receive(command.SerialNo, command.ProductId, command.WarehouseId);
        db.StockItems.Add(item);
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(item);
    }
}

public sealed record ReserveStockCommand(Guid StockItemId) : ICommand<StockItemDto>;

public sealed class ReserveStockHandler(IAppDbContext db) : ICommandHandler<ReserveStockCommand, StockItemDto>
{
    public async Task<StockItemDto> Handle(ReserveStockCommand command, CancellationToken ct)
    {
        var item = await db.StockItems.FirstAsync(s => s.Id == command.StockItemId, ct);
        item.Reserve();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(item);
    }
}

public sealed record DeliverStockCommand(Guid StockItemId, Guid OrderId) : ICommand<StockItemDto>;

public sealed class DeliverStockHandler(IAppDbContext db) : ICommandHandler<DeliverStockCommand, StockItemDto>
{
    public async Task<StockItemDto> Handle(DeliverStockCommand command, CancellationToken ct)
    {
        var item = await db.StockItems.Include(s => s.Movements).FirstAsync(s => s.Id == command.StockItemId, ct);
        item.DeliverToDealer(command.OrderId);
        db.MarkAdded(item.Movements.Last());
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(item);
    }
}

public sealed record GetStockByWarehouseQuery(Guid WarehouseId) : IQuery<IReadOnlyList<StockItemDto>>;

public sealed class GetStockByWarehouseHandler(IAppDbContext db) : IQueryHandler<GetStockByWarehouseQuery, IReadOnlyList<StockItemDto>>
{
    public async Task<IReadOnlyList<StockItemDto>> Handle(GetStockByWarehouseQuery query, CancellationToken ct)
    {
        return await db.StockItems.AsNoTracking()
            .Where(s => s.WarehouseId == query.WarehouseId)
            .Select(s => new StockItemDto(s.Id, s.SerialNo, s.ProductId, s.WarehouseId, s.Status))
            .ToListAsync(ct);
    }
}
