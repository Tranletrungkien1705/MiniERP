namespace MiniERP.Application.Abstractions;

public interface ICurrentUser
{
    string? Email { get; }
    string? PartnerCode { get; }
    bool IsAuthenticated { get; }
}
