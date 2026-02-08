# FIAP Cloud Gaming - Mediator

[![NuGet](https://img.shields.io/nuget/v/FiapCloudGaming.Mediator.svg)](https://www.nuget.org/packages/FiapCloudGaming.Mediator/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Implementacao leve do padrao Mediator para CQRS com commands, queries e notifications. Construido para arquitetura de microsservicos em .NET 10.

## Funcionalidades

- **Commands** - Executar operacoes com validacao e efeitos colaterais
- **Queries** - Recuperar dados com separacao de responsabilidades
- **Notifications** - Publicar eventos de dominio para multiplos handlers
- **Pipeline Behaviors** - Preocupacoes transversais (logging, validacao, etc.)
- **Eventos de Dominio** - Suporte nativo para despacho de eventos de dominio
- **Injecao de Dependencia** - Integracao com Microsoft.Extensions.DependencyInjection
- **Leve** - Dependencias minimas, performance maxima

## Instalacao

```bash
dotnet add package FiapCloudGaming.Mediator
```

Ou via NuGet Package Manager:

```powershell
Install-Package FiapCloudGaming.Mediator
```

## Inicio Rapido

### 1. Registrar o Mediator no Container de DI

```csharp
// Program.cs ou Startup.cs
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
```

### 2. Definir um Command

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

### 3. Definir uma Query

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

### 4. Definir uma Notification (Evento de Dominio)

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

// Voce pode ter multiplos handlers para a mesma notification
public class LogOrderHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly ILogger<LogOrderHandler> _logger;

    public LogOrderHandler(ILogger<LogOrderHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pedido {OrderId} criado para o usuario {UserId}",
            notification.OrderId, notification.UserId);
        await Task.CompletedTask;
    }
}
```

### 5. Usar o Mediator

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
        // Enviar Command
        var command = new CreateOrderCommand(request.UserId, request.Amount);
        var orderId = await _mediator.Send(command);

        // Publicar Notification (Evento de Dominio)
        await _mediator.Publish(new OrderCreatedEvent(orderId, request.UserId));

        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, orderId);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        // Enviar Query
        var query = new GetOrderQuery(id);
        var order = await _mediator.Send(query);

        return Ok(order);
    }
}
```

## Funcionalidades Avancadas

### Pipeline Behaviors

Adicione preocupacoes transversais como logging, validacao ou monitoramento de performance:

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
        _logger.LogInformation("Processando {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Processado {RequestName}", typeof(TRequest).Name);
        return response;
    }
}

// Registrar no DI
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

### Eventos de Dominio com NotificationEntity

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

        // Adicionar evento de dominio
        AddNotification(new OrderCreatedEvent(Id, UserId));
    }
}

// No seu repositorio/DbContext
public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
{
    // Despachar eventos de dominio antes de salvar
    await _mediator.DispatchNotificationsAsync(this, cancellationToken);

    await base.SaveChangesAsync(cancellationToken);
    return true;
}
```

## Arquitetura

Esta implementacao do mediator segue o padrao **CQRS** (Command Query Responsibility Segregation):

- **Commands**: Mutacoes que alteram estado (Criar, Atualizar, Deletar)
- **Queries**: Operacoes somente leitura que retornam dados
- **Notifications**: Eventos que podem ser tratados por multiplos handlers

## Por que usar este Mediator?

1. **Desacoplamento**: Controllers nao precisam conhecer implementacoes especificas de handlers
2. **Testabilidade**: Facilidade para testar handlers isoladamente
3. **Responsabilidade Unica**: Cada handler tem uma unica responsabilidade
4. **Principio Aberto/Fechado**: Adicione novos handlers sem modificar codigo existente
5. **Eventos de Dominio**: Suporte nativo para padroes de Domain-Driven Design
6. **Pipeline Behaviors**: Adicione preocupacoes transversais sem poluir a logica de negocio

## Performance

- Zero alocacoes para despacho de notifications
- Overhead minimo de reflection (delegates cacheados)
- Otimizado para cenarios de alta vazao
- Suporte a tratamento concorrente de notifications

## Dependencias

- `Microsoft.Extensions.DependencyInjection` (10.0.2+)
- `Scrutor` (7.0.0+) - Para varredura de assemblies

## Contribuicao

Contribuicoes sao bem-vindas! Sinta-se livre para enviar um Pull Request.

## Licenca

Este projeto esta licenciado sob a Licenca MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## Suporte

- **GitHub Issues**: [Reportar bugs ou solicitar funcionalidades](https://github.com/FIAP-Cloud-Gaming/Mediator/issues)
- **Discussions**: [Fazer perguntas ou compartilhar ideias](https://github.com/FIAP-Cloud-Gaming/Mediator/discussions)

---

**Feito pelo time FIAP Cloud Gaming**
