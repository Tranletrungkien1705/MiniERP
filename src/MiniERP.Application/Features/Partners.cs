using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Abstractions;
using MiniERP.Application.Cqrs;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Enums;

namespace MiniERP.Application.Features.Partners;

public sealed record PartnerDto(Guid Id, string Code, string Name, PartnerType Type, bool IsActive);

public sealed record CreatePartnerCommand(string Code, string Name, PartnerType Type) : ICommand<PartnerDto>;

public sealed class CreatePartnerHandler(IAppDbContext db) : ICommandHandler<CreatePartnerCommand, PartnerDto>
{
    public async Task<PartnerDto> Handle(CreatePartnerCommand command, CancellationToken ct)
    {
        var partner = Partner.Create(command.Code, command.Name, command.Type);
        db.Partners.Add(partner);
        await db.SaveChangesAsync(ct);
        return new PartnerDto(partner.Id, partner.Code, partner.Name, partner.Type, partner.IsActive);
    }
}

public sealed record ImportPartnerRow(string? Code, string? Name, PartnerType Type);
public sealed record ImportPartnersCommand(IReadOnlyList<ImportPartnerRow> Rows) : ICommand<ImportResultDto>;
public sealed record ImportResultDto(int Added, int Skipped, int Total);

public sealed class ImportPartnersHandler(IAppDbContext db) : ICommandHandler<ImportPartnersCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportPartnersCommand command, CancellationToken ct)
    {
        int added = 0, skipped = 0;
        var existingCodes = await db.Partners.AsNoTracking().Select(p => p.Code).ToListAsync(ct);
        var seen = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || string.IsNullOrWhiteSpace(row.Name)) { skipped++; continue; }
            var code = row.Code.Trim();
            if (!seen.Add(code)) { skipped++; continue; }
            db.Partners.Add(Partner.Create(code, row.Name.Trim(), row.Type));
            added++;
        }
        await db.SaveChangesAsync(ct);
        return new ImportResultDto(added, skipped, command.Rows.Count);
    }
}

public sealed record GetPartnersQuery(PartnerType? Type = null) : IQuery<IReadOnlyList<PartnerDto>>;

public sealed class GetPartnersHandler(IAppDbContext db) : IQueryHandler<GetPartnersQuery, IReadOnlyList<PartnerDto>>
{
    public async Task<IReadOnlyList<PartnerDto>> Handle(GetPartnersQuery query, CancellationToken ct)
    {
        var q = db.Partners.AsNoTracking().AsQueryable();
        if (query.Type is not null) q = q.Where(p => p.Type == query.Type);
        return await q
            .Select(p => new PartnerDto(p.Id, p.Code, p.Name, p.Type, p.IsActive))
            .ToListAsync(ct);
    }
}
