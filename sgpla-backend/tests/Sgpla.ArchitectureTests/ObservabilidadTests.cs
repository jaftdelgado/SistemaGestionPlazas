using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Sgpla.ArchitectureTests;

/// <summary>
/// Los logs de un módulo solo llevan identificadores y valores no personales.
/// Ver ESTANDAR_MODULOS.md, sección 11.
/// </summary>
public class ObservabilidadTests
{
    private static readonly Type[] TiposPermitidos =
    [
        typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(decimal), typeof(double),
        typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
    ];

    private static readonly string[] SufijosDeTextoPermitidos = ["Clave", "Codigo", "Estado"];

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void LoggerMessage_SoloRecibeIdentificadoresYValoresNoPersonales(string modulo)
    {
        const BindingFlags Todos = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var incumplen = Modulos.Ensamblados[modulo].GetTypes()
            .SelectMany(tipo => tipo.GetMethods(Todos))
            .Where(metodo => metodo.IsDefined(typeof(LoggerMessageAttribute), inherit: false))
            .SelectMany(metodo => metodo.GetParameters()
                .Where(parametro => !EsPermitido(parametro))
                .Select(parametro => $"{metodo.DeclaringType?.FullName}.{metodo.Name}({parametro.ParameterType.Name} {parametro.Name})"))
            .ToArray();

        incumplen.ShouldBeEmpty($"Parámetros de log no permitidos: {string.Join(", ", incumplen)}");
    }

    private static bool EsPermitido(ParameterInfo parametro)
    {
        var tipo = Nullable.GetUnderlyingType(parametro.ParameterType) ?? parametro.ParameterType;

        if (typeof(ILogger).IsAssignableFrom(tipo) || tipo == typeof(LogLevel) || typeof(Exception).IsAssignableFrom(tipo))
        {
            return true;
        }

        if (tipo.IsEnum || TiposPermitidos.Contains(tipo))
        {
            return true;
        }

        return tipo == typeof(string)
            && parametro.Name is not null
            && SufijosDeTextoPermitidos.Any(sufijo => parametro.Name.EndsWith(sufijo, StringComparison.OrdinalIgnoreCase));
    }
}
