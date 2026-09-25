using NetArchTest.Rules;

namespace Sgpla.ArchitectureTests;

/// <summary>
/// Ubicación, sellado y visibilidad de los tipos de cada módulo según su sufijo.
/// Ver ESTANDAR_MODULOS.md, secciones 3 a 5.
/// </summary>
public class ConvencionesTests
{
    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Handlers_ResidenEnApplicationYSonSellados(string modulo)
    {
        var ns = Modulos.Namespace(modulo);

        var ubicacion = Clases(modulo).And().HaveNameEndingWith("Handler")
            .Should().ResideInNamespace($"{ns}.Application")
            .GetResult();
        var sellado = Clases(modulo).And().HaveNameEndingWith("Handler")
            .Should().BeSealed()
            .GetResult();

        ubicacion.IsSuccessful.ShouldBeTrue(Describir(ubicacion));
        sellado.IsSuccessful.ShouldBeTrue(Describir(sellado));
    }

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
    public void Queries_ResidenEnInfrastructure(string modulo) =>
        ComprobarUbicacion(modulo, "Queries", "Infrastructure");

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
