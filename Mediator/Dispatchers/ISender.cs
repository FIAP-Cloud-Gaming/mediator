using Mediator.Commands;

namespace Mediator.Dispatchers;

public interface ISender
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task Send(ICommand command, CancellationToken cancellationToken = default);
}
