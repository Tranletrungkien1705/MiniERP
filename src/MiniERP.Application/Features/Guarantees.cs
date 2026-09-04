using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Partners;
using MiniERP.Domain.Entities;

namespace MiniERP.Application.Features.Guarantees;

// Import hàng loạt từ Pmt_Guarantee thật (2010.HTC). Nguồn không lưu trực tiếp DlrContractNo cho từng bảo lãnh
// (chỉ DealerCode+BankCode) — resolve ContractId bằng cách tìm 1 DealerContract đã import khớp CẢ DealerId lẫn BankId
// (không suy diễn hợp đồng cụ thể ngoài dữ liệu thật sẵn có). Bỏ qua nếu không tìm được khớp cả 2 chiều.
public sealed record ImportGuaranteeRow(string? DealerCode, string? BankCode, decimal Amount, DateOnly IssueDate, DateOnly ExpiryDate);
public sealed record ImportGuaranteesCommand(IReadOnlyList<ImportGuaranteeRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportGuaranteesHandler(IAppDbContext db) : ICommandHandler<ImportGuaranteesCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportGuaranteesCommand command, CancellationToken ct)
    {
        var partnersByCode = await db.Partners.AsNoTracking().ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);
        var contractIdByDealerBank = (await db.Contracts.AsNoTracking().Select(c => new { c.Id, c.DealerId, c.BankId }).ToListAsync(ct))
            .GroupBy(c => (c.DealerId, c.BankId))
            .ToDictionary(g => g.Key, g => g.First().Id);

        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.DealerCode) || string.IsNullOrWhiteSpace(row.BankCode) || row.Amount <= 0 || row.ExpiryDate <= row.IssueDate)
            { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.DealerCode.Trim(), out var dealerId)) { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.BankCode.Trim(), out var bankId)) { skipped++; continue; }
            if (!contractIdByDealerBank.TryGetValue((dealerId, bankId), out var contractId)) { skipped++; continue; }
            db.Guarantees.Add(Guarantee.Issue(contractId, bankId, row.Amount, row.IssueDate, row.ExpiryDate));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}
