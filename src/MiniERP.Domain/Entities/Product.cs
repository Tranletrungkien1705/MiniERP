using MiniERP.Domain.Common;

namespace MiniERP.Domain.Entities;

public sealed class Product : Entity
{
    public string ModelCode { get; private set; } = default!;
    public string SpecCode { get; private set; } = default!;
    public string ColorCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }

    private Product() { }

    public static Product Create(string modelCode, string specCode, string colorCode, string name, decimal unitPrice) => new()
    {
        ModelCode = modelCode,
        SpecCode = specCode,
        ColorCode = colorCode,
        Name = name,
        UnitPrice = unitPrice,
    };
}
