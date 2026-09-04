using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Application.Features.Partners;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Contracts;

public sealed record ContractDto(Guid Id, string ContractNo, Guid DealerId, decimal ContractValue, ContractStatus Status);

public sealed record CreateContractCommand(string ContractNo, Guid DealerId, Guid BankId, decimal ContractValue) : ICommand<ContractDto>;
public sealed record DealerSignContractCommand(Guid ContractId) : ICommand<ContractDto>;
public sealed record ApproveContractA1Command(Guid ContractId) : ICommand<ContractDto>;
public sealed record ApproveContractA2Command(Guid ContractId) : ICommand<ContractDto>;

file static class Mapper
{
    public static ContractDto ToDto(DealerContract c) => new(c.Id, c.ContractNo, c.DealerId, c.ContractValue, c.Status);
}

public sealed class CreateContractHandler(IAppDbContext db) : ICommandHandler<CreateContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(CreateContractCommand command, CancellationToken ct)
    {
        var contract = DealerContract.Create(command.ContractNo, command.DealerId, command.BankId, command.ContractValue);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(contract);
    }
}

public sealed class DealerSignContractHandler(IAppDbContext db) : ICommandHandler<DealerSignContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(DealerSignContractCommand command, CancellationToken ct)
    {
        var contract = await db.Contracts.FirstAsync(c => c.Id == command.ContractId, ct);
        contract.DealerSign();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(contract);
    }
}

public sealed class ApproveContractA1Handler(IAppDbContext db) : ICommandHandler<ApproveContractA1Command, ContractDto>
{
    public async Task<ContractDto> Handle(ApproveContractA1Command command, CancellationToken ct)
    {
        var contract = await db.Contracts.FirstAsync(c => c.Id == command.ContractId, ct);
        contract.ApproveA1();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(contract);
    }
}

public sealed class ApproveContractA2Handler(IAppDbContext db) : ICommandHandler<ApproveContractA2Command, ContractDto>
{
    public async Task<ContractDto> Handle(ApproveContractA2Command command, CancellationToken ct)
    {
        var contract = await db.Contracts.FirstAsync(c => c.Id == command.ContractId, ct);
        contract.ApproveA2();
        await db.SaveChangesAsync(ct);
        return Mapper.ToDto(contract);
    }
}

// Import hàng loạt từ Dlr_Contract thật (2010.HTC) — dealer/bank resolve theo Code có sẵn trong Partners (phải import Partners trước).
// Trạng thái nguồn (DlrCtrStatus A/C/F/P) không đủ tài liệu để suy diễn an toàn sang state machine Draft->DealerSigned->ApprovedA1->ApprovedA2,
// nên import giữ nguyên Draft (đúng luật chống bịa nghiệp vụ) — chỉ ContractNo/Dealer/Bank/ContractValue là dữ liệu thật.
public sealed record ImportContractRow(string? ContractNo, string? DealerCode, string? BankCode, decimal ContractValue);
public sealed record ImportContractsCommand(IReadOnlyList<ImportContractRow> Rows) : ICommand<ImportResultDto>;

public sealed class ImportContractsHandler(IAppDbContext db) : ICommandHandler<ImportContractsCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportContractsCommand command, CancellationToken ct)
    {
        var partnersByCode = await db.Partners.AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);
        var existingNos = new HashSet<string>(
            await db.Contracts.AsNoTracking().Select(c => c.ContractNo).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        int added = 0, skipped = 0;
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.ContractNo) || string.IsNullOrWhiteSpace(row.DealerCode) || string.IsNullOrWhiteSpace(row.BankCode)
                || row.ContractValue <= 0)
            { skipped++; continue; }
            if (!existingNos.Add(row.ContractNo.Trim())) { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.DealerCode.Trim(), out var dealerId)) { skipped++; continue; }
            if (!partnersByCode.TryGetValue(row.BankCode.Trim(), out var bankId)) { skipped++; continue; }
            db.Contracts.Add(DealerContract.Create(row.ContractNo.Trim(), dealerId, bankId, row.ContractValue));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}

public sealed record GetContractByIdQuery(Guid ContractId) : IQuery<ContractDto?>;

public sealed class GetContractByIdHandler(IAppDbContext db) : IQueryHandler<GetContractByIdQuery, ContractDto?>
{
    public async Task<ContractDto?> Handle(GetContractByIdQuery query, CancellationToken ct)
    {
        var contract = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == query.ContractId, ct);
        return contract is null ? null : Mapper.ToDto(contract);
    }
}
