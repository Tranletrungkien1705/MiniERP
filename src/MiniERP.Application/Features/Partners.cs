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
