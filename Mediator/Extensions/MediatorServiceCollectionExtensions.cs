using Mediator.Commands;
using Mediator.Dispatchers;
using Mediator.Notifications;
using Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Mediator.Extensions;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(
        this IServiceCollection services, 
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));

        services.AddScoped<IMediator, Mediator>();

        services.Scan(scan => scan
            .FromAssemblies(assemblies)

            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()

            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()

            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()

            .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddMediatorBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
    {
        services.Decorate(typeof(ICommandHandler<>), typeof(TBehavior));

        services.Decorate(typeof(ICommandHandler<,>), typeof(TBehavior));

        return services;
    }
}
