using System.Reflection;
using Microsoft.Extensions.Logging;
using Sgpla.Modules.Usuarios;

namespace Sgpla.UnitTests.Usuarios;

/// <summary>Los EventId de auditoría son fijos.</summary>
public sealed class AuditoriaTests
{
    [Fact]
    public void EventosDeAuditoria_DeclaranUnEventIdFijoYSinRepetir()
    {
        const BindingFlags Todos =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var eventIds = typeof(UsuariosModule).Assembly.GetTypes()
            .SelectMany(tipo => tipo.GetMethods(Todos))
            .Where(metodo => metodo.IsDefined(typeof(LoggerMessageAttribute), inherit: false))
            .Select(metodo => metodo.GetCustomAttribute<LoggerMessageAttribute>(inherit: false)!.EventId)
            .ToList();

        eventIds.ShouldNotBeEmpty();
        eventIds.ShouldAllBe(eventId => eventId != -1);
        eventIds.Distinct().Count().ShouldBe(eventIds.Count);
    }
}
