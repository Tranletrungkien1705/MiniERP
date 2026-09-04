using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Partners;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Orders;

public sealed record OrderLineDto(Guid Id, Guid ProductId, int DemandQty, int SupplyQty);
public sealed record OrderDto(Guid Id, string OrderNo, Guid DealerId, Guid ContractId, OrderStatus Status, IReadOnlyList<OrderLineDto> Lines);

file static class Mapper
{
    public static OrderDto ToDto(SalesOrder o) => new(
        o.Id, o.OrderNo, o.DealerId, o.ContractId, o.Status,
        [.. o.Lines.Select(l => new OrderLineDto(l.Id, l.ProductId, l.DemandQty, l.SupplyQty))]);
}

public sealed record CreateOrderCommand(string OrderNo, Guid DealerId, Guid ContractId) : ICommand<OrderDto>;

public sealed class CreateOrderHandler(IAppDbContext db) : ICommandHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var order = SalesOrder.Create(command.OrderNo, command.DealerId, command.ContractId);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(order);
    }
}

public sealed record AddDemandLineCommand(Guid OrderId, Guid ProductId, int DemandQty) : ICommand<OrderDto>;

public sealed class AddDemandLineHandler(IAppDbContext db) : ICommandHandler<AddDemandLineCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AddDemandLineCommand command, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == command.OrderId, ct);
        order.AddDemand(command.ProductId, command.DemandQty);
        db.MarkAdded(order.Lines.Last());
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(order);
    }
}

public sealed record SupplyLineCommand(Guid OrderId, Guid LineId, int SupplyQty) : ICommand<OrderDto>;

public sealed class SupplyLineHandler(IAppDbContext db) : ICommandHandler<SupplyLineCommand, OrderDto>
{
    public async Task<OrderDto> Handle(SupplyLineCommand command, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == command.OrderId, ct);
        order.Supply(command.LineId, command.SupplyQty);
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(order);
    }
}

public sealed record ApproveOrderA1Command(Guid OrderId) : ICommand<OrderDto>;

public sealed class ApproveOrderA1Handler(IAppDbContext db) : ICommandHandler<ApproveOrderA1Command, OrderDto>
{
    public async Task<OrderDto> Handle(ApproveOrderA1Command command, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == command.OrderId, ct);
        order.ApproveA1();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(order);
    }
}

public sealed record ApproveOrderA2Command(Guid OrderId) : ICommand<OrderDto>;

public sealed class ApproveOrderA2Handler(IAppDbContext db) : ICommandHandler<ApproveOrderA2Command, OrderDto>
{
    public async Task<OrderDto> Handle(ApproveOrderA2Command command, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == command.OrderId, ct);
        order.ApproveA2();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(order);
    }
}

public sealed record CompleteOrderCommand(Guid OrderId) : ICommand<OrderDto>;

public sealed class CompleteOrderHandler(IAppDbContext db) : ICommandHandler<CompleteOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CompleteOrderCommand command, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == command.OrderId, ct);
        order.Complete();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(order);
    }
}

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;

public sealed class GetOrderByIdHandler(IAppDbContext db) : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery query, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId, ct);
        return order is null ? null : Mapper.ToDto(order);
    }
}

// Import hàng loạt từ DLS_Deal (header) + dòng xe (Model/Spec/Color qua Car_Car) — resolve Dealer/Contract/Product theo Code, giữ Status=Demand (trạng thái tạo tự nhiên).
public sealed record ImportOrderLine(string? ModelCode, string? SpecCode, string? ColorCode, int DemandQty);
public sealed record ImportOrderRow(string? OrderNo, string? DealerCode, string? ContractNo, IReadOnlyList<ImportOrderLine> Lines);
public sealed record ImportOrdersCommand(IReadOnlyList<ImportOrderRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportOrdersHandler(IAppDbContext db) : ICommandHandler<ImportOrdersCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportOrdersCommand command, CancellationToken ct)
    {
        var partnersByCode = await db.Partners.AsNoTracking().ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);
        var contractsByNo = await db.Contracts.AsNoTracking().ToDictionaryAsync(c => c.ContractNo, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);
        var productsByKey = (await db.Products.AsNoTracking().ToListAsync(ct))
            .GroupBy(p => $"{p.ModelCode}|{p.SpecCode}|{p.ColorCode}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var existingNos = new HashSet<string>(await db.Orders.AsNoTracking().Select(o => o.OrderNo).ToListAsync(ct), StringComparer.OrdinalIgnoreCase);

        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.OrderNo) || string.IsNullOrWhiteSpace(row.DealerCode) || string.IsNullOrWhiteSpace(row.ContractNo)
                || row.Lines is null || row.Lines.Count == 0)
            { skipped++; continue; }
            if (!existingNos.Add(row.OrderNo.Trim())) { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.DealerCode.Trim(), out var dealerId)) { skipped++; continue; }
            if (!contractsByNo.TryGetValue(row.ContractNo.Trim(), out var contractId)) { skipped++; continue; }

            var order = SalesOrder.Create(row.OrderNo.Trim(), dealerId, contractId);
            var anyLine = false;
            foreach (var line in row.Lines)
            {
                var key = $"{line.ModelCode}|{line.SpecCode}|{line.ColorCode}";
                if (!productsByKey.TryGetValue(key, out var productId) || line.DemandQty <= 0) continue;
                order.AddDemand(productId, line.DemandQty);
                anyLine = true;
            }
            if (!anyLine) { skipped++; continue; }
            db.Orders.Add(order);
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}

public sealed record GetOrdersByDealerQuery(Guid DealerId) : IQuery<IReadOnlyList<OrderDto>>;

public sealed class GetOrdersByDealerHandler(IAppDbContext db) : IQueryHandler<GetOrdersByDealerQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersByDealerQuery query, CancellationToken ct)
    {
        var orders = await db.Orders.AsNoTracking().Include(o => o.Lines)
            .Where(o => o.DealerId == query.DealerId).ToListAsync(ct);
        return [.. orders.Select(Mapper.ToDto)];
    }
}
