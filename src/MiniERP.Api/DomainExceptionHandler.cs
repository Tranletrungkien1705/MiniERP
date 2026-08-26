using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Domain.Exceptions;

namespace MiniERP.Api;

// Vi phạm business rule (DomainException — vd duyệt hợp đồng sai thứ tự trạng thái) là lỗi INPUT của
// caller, phải trả 400 kèm message rõ ràng — không phải 500. Không bắt exception khác (để lộ đúng 500
// cho lỗi hạ tầng thật, dễ chẩn đoán).
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domainException) return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Vi phạm quy tắc nghiệp vụ",
            Detail = domainException.Message,
        }, ct);

        return true;
    }
}
