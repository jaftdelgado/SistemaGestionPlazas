using NetArchTest.Rules;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.ArchitectureTests;

/// <summary>
/// Ubicación, sellado y visibilidad de los tipos de cada módulo según su sufijo o su rol.
/// </summary>
public class ConvencionesTests
{
    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void CommandHandlers_ResidenEnApplicationYSonSellados(string modulo) =>
        ComprobarHandlers(modulo, "Application", typeof(ICommandHandler<>), typeof(ICommandHandler<,>));

    /// <summary>Una consulta es un adaptador de lectura: proyecta desde <c>SgplaDbContext</c> sin pasar por un puerto.</summary>
    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void QueryHandlers_ResidenEnInfrastructureYSonSellados(string modulo) =>
        ComprobarHandlers(modulo, "Infrastructure", typeof(IQueryHandler<,>));

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Validators_ResidenEnApplication(string modulo) =>
        ComprobarUbicacion(modulo, "Validator", "Application");

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Configurations_ResidenEnInfrastructure(string modulo) =>
        ComprobarUbicacion(modulo, "Configuration", "Infrastructure");

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Repositories_ResidenEnInfrastructure(string modulo) =>
        ComprobarUbicacion(modulo, "Repository", "Infrastructure");

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Endpoints_ResidenEnEndpoints(string modulo) =>
        ComprobarUbicacion(modulo, "Endpoints", "Endpoints");

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Tipos_SoloSonPublicosElModuloYSusContratos(string modulo)
    {
        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .That().DoNotResideInNamespace(Modulos.NamespaceContratos(modulo))
            .And().DoNotHaveName(Modulos.NombreClaseModulo(modulo))
            .Should().NotBePublic()
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Describir(resultado));
    }

    /// <summary>Las interfaces (puertos) quedan fuera: se declaran en Application.</summary>
    private static PredicateList Clases(string modulo) =>
        Types.InAssembly(Modulos.Ensamblados[modulo]).That().AreClasses();

    /// <summary>
    /// Por reflexión y no por sufijo: así también cubre los handlers genéricos (<c>ListarCatalogoFijoHandler`1</c>),
    /// cuyo nombre no termina en <c>Handler</c>.
    /// </summary>
    private static void ComprobarHandlers(string modulo, string capa, params Type[] interfaces)
    {
        var espacio = $"{Modulos.Namespace(modulo)}.{capa}";

        var incumplen = Modulos.Ensamblados[modulo].GetTypes()
            .Where(tipo => tipo.IsClass && tipo.GetInterfaces().Any(interfaz =>
                interfaz.IsGenericType && interfaces.Contains(interfaz.GetGenericTypeDefinition())))
            .Where(tipo => !tipo.IsSealed
                || tipo.Namespace is null
                || !(tipo.Namespace == espacio || tipo.Namespace.StartsWith($"{espacio}.", StringComparison.Ordinal)))
            .Select(tipo => tipo.FullName)
            .ToArray();

        incumplen.ShouldBeEmpty($"Handlers fuera de {capa} o sin sellar: {string.Join(", ", incumplen)}");
    }

    private static void ComprobarUbicacion(string modulo, string sufijo, string capa)
    {
        var resultado = Clases(modulo).And().HaveNameEndingWith(sufijo)
            .Should().ResideInNamespace($"{Modulos.Namespace(modulo)}.{capa}")
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Describir(resultado));
    }

    private static string Describir(NetArchTest.Rules.TestResult resultado) =>
        $"Tipos que incumplen la regla: {string.Join(", ", resultado.FailingTypeNames ?? [])}";
}
