using Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Wrappers;

public abstract class QueryHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public sealed class QueryHandlerWrapperImpl<TQuery, TResponse> : QueryHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return handler.Handle((TQuery)query, cancellationToken);
    }
}
