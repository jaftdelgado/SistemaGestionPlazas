using System.Globalization;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Handlers;

public static class HandlersExtensions
{
    private static readonly Dictionary<Type, Type> Decoradores = new()
    {
        [typeof(ICommandHandler<>)] = typeof(ValidacionCommandDecorator<>),
        [typeof(ICommandHandler<,>)] = typeof(ValidacionCommandDecorator<,>),
        [typeof(IQueryHandler<,>)] = typeof(ValidacionQueryDecorator<,>),
    };

    /// <summary>
    /// Registra los handlers y validators de un módulo. Cada handler se resuelve por su interfaz envuelto en el
    /// decorador de validación, así que los validators se ejecutan antes de cualquier handler.
    /// </summary>
    public static IServiceCollection AddHandlersModulo(this IServiceCollection services, Assembly ensamblado)
    {
        ArgumentNullException.ThrowIfNull(ensamblado);

        // Mensajes de FluentValidation en español, sin depender de la cultura del contenedor.
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("es");

        services.AddValidatorsFromAssembly(ensamblado, ServiceLifetime.Scoped, includeInternalTypes: true);

        var handlers = ensamblado.GetTypes()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var handler in handlers)
        {
            foreach (var interfaz in handler.GetInterfaces().Where(EsHandler))
            {
                var decorador = Decoradores[interfaz.GetGenericTypeDefinition()].MakeGenericType(interfaz.GetGenericArguments());

                services.TryAddScoped(handler);
                services.AddScoped(interfaz, proveedor => ActivatorUtilities.CreateInstance(
                    proveedor, decorador, proveedor.GetRequiredService(handler)));
            }
        }

        return services;
    }

    private static bool EsHandler(Type interfaz) =>
        interfaz.IsGenericType && Decoradores.ContainsKey(interfaz.GetGenericTypeDefinition());
}
