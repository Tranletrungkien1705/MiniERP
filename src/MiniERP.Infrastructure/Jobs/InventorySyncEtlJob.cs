using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniERP.Domain.Enums;
using MiniERP.Infrastructure.Persistence;

namespace MiniERP.Infrastructure.Jobs;

// ETL định kỳ (mẫu InBrand ELTS): Extract tồn kho theo kho -> Transform (đếm theo trạng thái) ->
// Load ra bảng tổng hợp / log cảnh báo. Ở đây minh hoạ Extract+Transform, Load in ra log (thay cho
// đẩy sang Elasticsearch/DW thật). BackgroundService = cách chuẩn .NET để chạy job nền, thay Windows
// Service/Task Scheduler riêng lẻ như hệ WinForms cũ.
public sealed class InventorySyncEtlJob(
    IServiceScopeFactory scopeFactory,
    ILogger<InventorySyncEtlJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "InventorySyncEtlJob thất bại.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Extract
        var byWarehouse = await db.StockItems.AsNoTracking()
            .GroupBy(s => s.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                InStock = g.Count(s => s.Status == StockItemStatus.InStock),
                Reserved = g.Count(s => s.Status == StockItemStatus.Reserved),
            })
            .ToListAsync(ct);

        // Transform + Load (demo: log cảnh báo tồn thấp; hệ thật sẽ ghi bảng Summary/đẩy dashboard)
        foreach (var row in byWarehouse.Where(r => r.InStock < 5))
        {
            logger.LogWarning(
                "ETL cảnh báo: kho {WarehouseId} tồn thấp InStock={InStock} Reserved={Reserved}",
                row.WarehouseId, row.InStock, row.Reserved);
        }

        logger.LogInformation("InventorySyncEtlJob chạy xong: {Count} kho được quét.", byWarehouse.Count);
    }
}
