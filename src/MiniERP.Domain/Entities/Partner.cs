using MiniERP.Domain.Common;
using MiniERP.Domain.Enums;

namespace MiniERP.Domain.Entities;

public sealed class Partner : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public PartnerType Type { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Partner() { }

    public static Partner Create(string code, string name, PartnerType type) => new()
    {
        Code = code,
        Name = name,
        Type = type,
    };

    public void Deactivate() { IsActive = false; Touch(); }
}
