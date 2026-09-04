using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Partners;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Payments;

// Import hàng loạt từ DLS_Deal thật (cùng nguồn đã dùng cho MiniPay — Price là tiền cọc thật, không phải giá xe).
// Nguồn không lưu DlrContractNo khớp trực tiếp tập Contract đã import (đã kiểm chứng độ thưa ở SalesOrder) —
// resolve ContractId qua DealerId+BankId (cùng dealer/ngân hàng thật giao dịch), không suy diễn hợp đồng cụ thể.
public sealed record ImportPaymentRow(string? DealerCode, string? BankCode, decimal Amount, DateOnly PaidDate);
public sealed record ImportPaymentsCommand(IReadOnlyList<ImportPaymentRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportPaymentsHandler(IAppDbContext db) : ICommandHandler<ImportPaymentsCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportPaymentsCommand command, CancellationToken ct)
    {
        var partnersByCode = await db.Partners.AsNoTracking().ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);
        var contractIdByDealerBank = (await db.Contracts.AsNoTracking().Select(c => new { c.Id, c.DealerId, c.BankId }).ToListAsync(ct))
            .GroupBy(c => (c.DealerId, c.BankId))
            .ToDictionary(g => g.Key, g => g.First().Id);
        // Entity không có mã tự nhiên — dedupe theo tổ hợp ContractId+Amount+PaidDate để import lại không nhân đôi.
        var existingPayments = await db.Payments.AsNoTracking().Select(p => new { p.ContractId, p.Amount, p.PaidDate }).ToListAsync(ct);
        var existingKeys = new HashSet<(Guid, decimal, DateOnly)>(existingPayments.Select(p => (p.ContractId, p.Amount, p.PaidDate)));

        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.DealerCode) || string.IsNullOrWhiteSpace(row.BankCode) || row.Amount <= 0)
            { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.DealerCode.Trim(), out var dealerId)) { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.BankCode.Trim(), out var bankId)) { skipped++; continue; }
            if (!contractIdByDealerBank.TryGetValue((dealerId, bankId), out var contractId)) { skipped++; continue; }
            var key = (contractId, row.Amount, row.PaidDate);
            if (!existingKeys.Add(key)) { skipped++; continue; }
            db.Payments.Add(Payment.Record(contractId, PaymentType.Deposit, row.Amount, row.PaidDate));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}
