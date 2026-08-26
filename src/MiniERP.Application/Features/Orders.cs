using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
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
