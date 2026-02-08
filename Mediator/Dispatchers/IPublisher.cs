namespace Mediator.Dispatchers;

public interface IPublisher
{
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
