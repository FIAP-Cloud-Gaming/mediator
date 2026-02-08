using Mediator.Queries;

namespace Mediator.Dispatchers;

public interface IReceiver
{
    Task<TResponse> Receive<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
