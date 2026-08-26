using Microsoft.EntityFrameworkCore;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;
using MiniERP.Infrastructure.Persistence;

namespace MiniERP.Api;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Partners.AnyAsync()) return;

        var principal = Partner.Create("HTC", "Honda Trading Company", PartnerType.Principal);
        var dealer = Partner.Create("VN001", "Đại lý Demo", PartnerType.Dealer);
        var bank = Partner.Create("VCB", "Vietcombank", PartnerType.Bank);
        db.Partners.AddRange(principal, dealer, bank);

        var product = Product.Create("SH160", "STD", "RED", "Honda SH 160i", 85_000_000m);
        db.Products.Add(product);

        var warehouse = Warehouse.Create("WH01", "Kho trung tâm");
        db.Warehouses.Add(warehouse);

        await db.SaveChangesAsync();
    }
}
