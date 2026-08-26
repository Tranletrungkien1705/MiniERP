using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
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

public sealed record GetContractByIdQuery(Guid ContractId) : IQuery<ContractDto?>;

public sealed class GetContractByIdHandler(IAppDbContext db) : IQueryHandler<GetContractByIdQuery, ContractDto?>
{
    public async Task<ContractDto?> Handle(GetContractByIdQuery query, CancellationToken ct)
    {
        var contract = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == query.ContractId, ct);
        return contract is null ? null : Mapper.ToDto(contract);
    }
}
