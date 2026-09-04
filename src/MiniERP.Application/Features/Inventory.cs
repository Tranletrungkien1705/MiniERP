using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Partners;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Inventory;

public sealed record StockItemDto(Guid Id, string SerialNo, Guid ProductId, Guid WarehouseId, StockItemStatus Status);

// Import hàng loạt từ nguồn thật (Mst_Storage cho Warehouse, Mst_CarPrice cho Product, Car_VIN cho StockItem).
public sealed record ImportWarehouseRow(string? Code, string? Name);
public sealed record ImportWarehousesCommand(IReadOnlyList<ImportWarehouseRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportWarehousesHandler(IAppDbContext db) : ICommandHandler<ImportWarehousesCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportWarehousesCommand command, CancellationToken ct)
    {
        var seen = new HashSet<string>(await db.Warehouses.AsNoTracking().Select(w => w.Code).ToListAsync(ct), StringComparer.OrdinalIgnoreCase);
        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || string.IsNullOrWhiteSpace(row.Name) || !seen.Add(row.Code.Trim())) { skipped++; continue; }
            db.Warehouses.Add(Warehouse.Create(row.Code.Trim(), row.Name.Trim()));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}

public sealed record ImportProductRow(string? ModelCode, string? SpecCode, string? ColorCode, string? Name, decimal UnitPrice);
public sealed record ImportProductsCommand(IReadOnlyList<ImportProductRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportProductsHandler(IAppDbContext db) : ICommandHandler<ImportProductsCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportProductsCommand command, CancellationToken ct)
    {
        var seen = new HashSet<string>(
            (await db.Products.AsNoTracking().Select(p => new { p.ModelCode, p.SpecCode, p.ColorCode }).ToListAsync(ct))
                .Select(p => $"{p.ModelCode}|{p.SpecCode}|{p.ColorCode}"), StringComparer.OrdinalIgnoreCase);
        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.ModelCode) || string.IsNullOrWhiteSpace(row.SpecCode) || string.IsNullOrWhiteSpace(row.ColorCode))
            { skipped++; continue; }
            var key = $"{row.ModelCode.Trim()}|{row.SpecCode.Trim()}|{row.ColorCode.Trim()}";
            if (!seen.Add(key)) { skipped++; continue; }
            db.Products.Add(Product.Create(row.ModelCode.Trim(), row.SpecCode.Trim(), row.ColorCode.Trim(), row.Name?.Trim() ?? row.ModelCode.Trim(), row.UnitPrice));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}

// StockItem resolve Product theo ModelCode+SpecCode+ColorCode, Warehouse theo Code — phải import Product+Warehouse trước.
public sealed record ImportStockItemRow(string? SerialNo, string? ModelCode, string? SpecCode, string? ColorCode, string? WarehouseCode);
public sealed record ImportStockItemsCommand(IReadOnlyList<ImportStockItemRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportStockItemsHandler(IAppDbContext db) : ICommandHandler<ImportStockItemsCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportStockItemsCommand command, CancellationToken ct)
    {
        var productsByKey = (await db.Products.AsNoTracking().ToListAsync(ct))
            .GroupBy(p => $"{p.ModelCode}|{p.SpecCode}|{p.ColorCode}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var warehousesByCode = await db.Warehouses.AsNoTracking().ToDictionaryAsync(w => w.Code, w => w.Id, StringComparer.OrdinalIgnoreCase, ct);
        var existingSerials = new HashSet<string>(await db.StockItems.AsNoTracking().Select(s => s.SerialNo).ToListAsync(ct), StringComparer.OrdinalIgnoreCase);

        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.SerialNo) || !existingSerials.Add(row.SerialNo.Trim())) { skipped++; continue; }
            var key = $"{row.ModelCode}|{row.SpecCode}|{row.ColorCode}";
            if (!productsByKey.TryGetValue(key, out var productId)) { skipped++; continue; }
            if (row.WarehouseCode is null || !warehousesByCode.TryGetValue(row.WarehouseCode.Trim(), out var warehouseId)) { skipped++; continue; }
            db.StockItems.Add(StockItem.Receive(row.SerialNo.Trim(), productId, warehouseId));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}

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
