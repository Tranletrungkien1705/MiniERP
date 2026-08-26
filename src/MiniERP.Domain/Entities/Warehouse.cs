using MiniERP.Domain.Common;

namespace MiniERP.Domain.Entities;

public sealed class Warehouse : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid? OwnerPartnerId { get; private set; }

    private Warehouse() { }

    public static Warehouse Create(string code, string name, Guid? ownerPartnerId = null) => new()
    {
        Code = code,
        Name = name,
        OwnerPartnerId = ownerPartnerId,
    };
}
