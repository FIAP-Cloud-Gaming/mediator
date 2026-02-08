namespace Mediator;

public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, CommandHandlerBase> _commandHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> _notificationHandlers = new();
    private static readonly ConcurrentDictionary<Type, object> _queryHandlers = new();

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handler = _notificationHandlers.GetOrAdd(notification.GetType(), static requestType =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(requestType);

            var wrapper = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");

            return (NotificationHandlerWrapper)wrapper;
        });

        return handler.Handle(notification, serviceProvider, cancellationToken);
    }

    public Task<TResponse> Receive<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var handler = (QueryHandlerWrapper<TResponse>)_queryHandlers.GetOrAdd(query.GetType(), static queryType =>
        {
            var responseType = queryType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>))
                .GetGenericArguments()[0];

            var wrapperType = typeof(QueryHandlerWrapperImpl<,>).MakeGenericType(queryType, responseType);

            var wrapper = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper type for {queryType}");

            return wrapper;
        });

        return handler.Handle(query, serviceProvider, cancellationToken);
    }

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = (CommandHandlerWrapper<TResponse>)_commandHandlers.GetOrAdd(command.GetType(), static requestType =>
        {
            var wrapperType = typeof(CommandHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
            
            var wrapper = Activator.CreateInstance(wrapperType) 
                ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
            
            return (CommandHandlerBase)wrapper;
        });

        return handler.Handle(command, serviceProvider, cancellationToken);
    }

    public Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = (CommandHandlerWrapper)_commandHandlers.GetOrAdd(command.GetType(), static requestType =>
        {
            var wrapperType = typeof(CommandHandlerWrapperImpl<>).MakeGenericType(requestType);

            var wrapper = Activator.CreateInstance(wrapperType) 
                ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");

            return (CommandHandlerBase)wrapper;
        });

        return handler.Handle(command, serviceProvider, cancellationToken);
    }
}