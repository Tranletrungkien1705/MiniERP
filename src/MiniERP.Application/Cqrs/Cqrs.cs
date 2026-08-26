namespace MiniERP.Application.Cqrs;

public interface ICommand<TResult>;
public interface IQuery<TResult>;

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}

public interface ISender
{
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
