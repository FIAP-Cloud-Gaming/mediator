using Mediator.Dispatchers;

namespace Mediator;

public interface IMediator : ISender, IPublisher, IReceiver;
