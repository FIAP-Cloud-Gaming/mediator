# FIAP Cloud Gaming - Mediator

[![NuGet](https://img.shields.io/nuget/v/FiapCloudGaming.Mediator.svg)](https://www.nuget.org/packages/FiapCloudGaming.Mediator/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Lightweight mediator implementation for CQRS pattern with commands, queries and notifications. Built for .NET 10 microservices architecture.

## Features

- ✅ **Commands** - Execute operations with validation and side effects
- ✅ **Queries** - Retrieve data with separation of concerns
- ✅ **Notifications** - Publish domain events to multiple handlers
- ✅ **Pipeline Behaviors** - Cross-cutting concerns (logging, validation, etc.)
- ✅ **Domain Events** - Built-in support for dispatching domain events
- ✅ **Dependency Injection** - First-class integration with Microsoft.Extensions.DependencyInjection
- ✅ **Lightweight** - Minimal dependencies, maximum performance

## Installation

```bash
dotnet add package FiapCloudGaming.Mediator
```

Or via NuGet Package Manager:

```powershell
Install-Package FiapCloudGaming.Mediator
```

## Quick Start

### 1. Register Mediator in DI Container

```csharp
// Program.cs or Startup.cs
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
```

### 2. Define a Command

```csharp
// Commands/CreateOrderCommand.cs
public record CreateOrderCommand(string UserId, decimal Amount) : ICommand<Guid>;

// Commands/CreateOrderCommandHandler.cs
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;

    public CreateOrderCommandHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new Order(command.UserId, command.Amount);
        await _repository.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
```

### 3. Define a Query

```csharp
// Queries/GetOrderQuery.cs
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Queries/GetOrderQueryHandler.cs
public class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, OrderDto>
{
    private readonly IOrderRepository _repository;

    public GetOrderQueryHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderDto> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(query.OrderId, cancellationToken);
        return new OrderDto(order.Id, order.UserId, order.Amount);
    }
}
```

### 4. Define a Notification (Domain Event)

```csharp
// Events/OrderCreatedEvent.cs
public record OrderCreatedEvent(Guid OrderId, string UserId) : INotification;

// Events/OrderCreatedEventHandler.cs
public class SendEmailHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;

    public SendEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationAsync(notification.UserId, notification.OrderId);
    }
}

// You can have multiple handlers for the same notification
public class LogOrderHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly ILogger<LogOrderHandler> _logger;

    public LogOrderHandler(ILogger<LogOrderHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderId} created for user {UserId}",
            notification.OrderId, notification.UserId);
        await Task.CompletedTask;
    }
}
```

### 5. Use Mediator

```csharp
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Send Command
        var command = new CreateOrderCommand(request.UserId, request.Amount);
        var orderId = await _mediator.Send(command);

        // Publish Notification (Domain Event)
        await _mediator.Publish(new OrderCreatedEvent(orderId, request.UserId));

        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, orderId);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        // Send Query
        var query = new GetOrderQuery(id);
        var order = await _mediator.Send(query);

        return Ok(order);
    }
}
```

## Advanced Features

### Pipeline Behaviors

Add cross-cutting concerns like logging, validation, or performance monitoring:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}

// Register in DI
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

### Domain Events with NotificationEntity

```csharp
public class Order : NotificationEntity
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public decimal Amount { get; private set; }

    public Order(string userId, decimal amount)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;

        // Add domain event
        AddNotification(new OrderCreatedEvent(Id, UserId));
    }
}

// In your repository/DbContext
public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
{
    // Dispatch domain events before saving
    await _mediator.DispatchNotificationsAsync(this, cancellationToken);

    await base.SaveChangesAsync(cancellationToken);
    return true;
}
```

## Architecture

This mediator implementation follows the **CQRS** (Command Query Responsibility Segregation) pattern:

- **Commands**: Mutations that change state (Create, Update, Delete)
- **Queries**: Read-only operations that return data
- **Notifications**: Events that can be handled by multiple handlers

## Why Use This Mediator?

1. **Decoupling**: Controllers don't need to know about specific handler implementations
2. **Testability**: Easy to test handlers in isolation
3. **Single Responsibility**: Each handler has one job
4. **Open/Closed Principle**: Add new handlers without modifying existing code
5. **Domain Events**: First-class support for domain-driven design patterns
6. **Pipeline Behaviors**: Add cross-cutting concerns without polluting business logic

## Performance

- Zero allocations for notification dispatch
- Minimal reflection overhead (cached delegates)
- Optimized for high-throughput scenarios
- Supports concurrent notification handling

## Dependencies

- `Microsoft.Extensions.DependencyInjection` (10.0.2+)
- `Scrutor` (7.0.0+) - For assembly scanning

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/fiap-cloud-gaming/Mediator/issues)
- **Discussions**: [Ask questions or share ideas](https://github.com/fiap-cloud-gaming/Mediator/discussions)

---

**Made with ❤️ by FIAP Cloud Gaming Team**
