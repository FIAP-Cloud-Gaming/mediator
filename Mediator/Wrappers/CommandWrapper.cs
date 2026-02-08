using Mediator.Behaviors;
using Mediator.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Wrappers;

public abstract class CommandHandlerBase
{
    public abstract Task Handle(object handler, IServiceProvider serviceProvider, 
        CancellationToken cancellationToken);
}

public abstract class CommandHandlerWrapper<TResponse> : CommandHandlerBase
{
    public abstract Task<TResponse> Handle(ICommand<TResponse> handler, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public abstract class CommandHandlerWrapper : CommandHandlerBase
{
    public abstract Task Handle(ICommand handler, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public class CommandHandlerWrapperImpl<TRequest, TResponse> : CommandHandlerWrapper<TResponse>
    where TRequest : ICommand<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) 
            => await Handle((ICommand<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<TResponse> Handle(ICommand<TResponse> request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<ICommandHandler<TRequest, TResponse>>()
            .Handle((TRequest)request, t == default ? cancellationToken : t);

        return serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>)Handler,
                (next, pipeline) => (t) => pipeline.Handle((TRequest)request, next, t == default ? cancellationToken : t))(cancellationToken);
    }
}

public class CommandHandlerWrapperImpl<TRequest> : CommandHandlerWrapper
    where TRequest : ICommand
{
    public override async Task Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((ICommand)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task Handle(ICommand request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        async Task Handler(CancellationToken t = default)
        {
            await serviceProvider.GetRequiredService<ICommandHandler<TRequest>>()
                .Handle((TRequest)request, t == default ? cancellationToken : t);
        }

        return serviceProvider
            .GetServices<IPipelineBehavior<TRequest>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate)Handler,
                (next, pipeline) => (t) => pipeline.Handle((TRequest)request, next, t == default ? cancellationToken : t))(cancellationToken);
    }
}