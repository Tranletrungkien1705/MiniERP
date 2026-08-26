using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MiniERP.Application.Cqrs;

public sealed class Dispatcher(IServiceProvider services) : ISender
{
    private static readonly Type CommandHandlerType = typeof(ICommandHandler<,>);
    private static readonly Type QueryHandlerType = typeof(IQueryHandler<,>);

    public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default) =>
        Invoke<TResult>(CommandHandlerType, command, ct);

    public Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default) =>
        Invoke<TResult>(QueryHandlerType, query, ct);

    private Task<TResult> Invoke<TResult>(Type openHandlerType, object request, CancellationToken ct)
    {
        var handlerType = openHandlerType.MakeGenericType(request.GetType(), typeof(TResult));
        var handler = services.GetRequiredService(handlerType);
        var method = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handle method not found on {handlerType}.");
        return (Task<TResult>)method.Invoke(handler, [request, ct])!;
    }
}
