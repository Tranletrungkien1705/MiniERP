using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Cqrs;

namespace MiniERP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISender, Dispatcher>();

        var assembly = typeof(DependencyInjection).Assembly;
        var handlerInterfaceTypes = new[] { typeof(ICommandHandler<,>), typeof(IQueryHandler<,>) };

        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definition = iface.GetGenericTypeDefinition();
                if (handlerInterfaceTypes.Contains(definition))
                {
                    services.AddScoped(iface, type);
                }
            }
        }

        return services;
    }
}
