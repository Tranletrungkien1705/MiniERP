using Microsoft.EntityFrameworkCore;
using MiniERP.Domain.Entities;

namespace MiniERP.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Partner> Partners { get; }
    DbSet<Product> Products { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<DealerContract> Contracts { get; }
    DbSet<SalesOrder> Orders { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<Guarantee> Guarantees { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Invoice> Invoices { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // Guid key được set client-side (Entity.Id = Guid.NewGuid() lúc construct) khiến EF Core không tự
    // suy được Added-vs-Modified khi child entity mới được thêm vào 1 collection navigation của aggregate
    // ĐÃ tracked (load qua query rồi mutate, khác với Add() cả aggregate mới) — phải đánh dấu tường minh.
    void MarkAdded<TEntity>(TEntity entity) where TEntity : class;
}
