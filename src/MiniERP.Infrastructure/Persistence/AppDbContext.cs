using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Domain.Entities;

namespace MiniERP.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<DealerContract> Contracts => Set<DealerContract>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Guarantee> Guarantees => Set<Guarantee>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public void MarkAdded<TEntity>(TEntity entity) where TEntity : class => Entry(entity).State = EntityState.Added;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Partner>(e => e.HasIndex(p => p.Code).IsUnique());
        modelBuilder.Entity<Warehouse>(e => e.HasIndex(w => w.Code).IsUnique());
        modelBuilder.Entity<DealerContract>(e => e.HasIndex(c => c.ContractNo).IsUnique());
        modelBuilder.Entity<StockItem>(e => e.HasIndex(s => s.SerialNo).IsUnique());
        modelBuilder.Entity<Invoice>(e => e.HasIndex(i => i.InvoiceNo).IsUnique());

        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.HasIndex(o => o.OrderNo).IsUnique();
            e.Metadata.FindNavigation(nameof(SalesOrder.Lines))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SalesOrderLine>(e => e.HasKey(l => l.Id));

        modelBuilder.Entity<StockItem>(e =>
        {
            e.Metadata.FindNavigation(nameof(StockItem.Movements))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<StockMovement>(e => e.HasKey(m => m.Id));

        base.OnModelCreating(modelBuilder);
    }
}
