using NetArchTest.Rules;

namespace Sgpla.ArchitectureTests;

/// <summary>
/// Reglas de Clean Architecture dentro de cada módulo. Las capas son carpetas
/// (y por tanto namespaces) del mismo proyecto: Domain, Application, Infrastructure y Endpoints.
/// </summary>
public class CapasTests
{
    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Domain_NoDependeDeOtrasCapasNiDeFrameworks(string modulo)
    {
        var ns = Modulos.Namespace(modulo);

        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .That().ResideInNamespace($"{ns}.Domain")
            .ShouldNot().HaveDependencyOnAny(
                $"{ns}.Application",
                $"{ns}.Infrastructure",
                $"{ns}.Endpoints",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "FluentValidation",
                "Sgpla.BuildingBlocks.Application",
                "Sgpla.BuildingBlocks.Infrastructure")
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Application_NoDependeDeInfrastructureEndpointsNiEfCore(string modulo)
    {
        var ns = Modulos.Namespace(modulo);

        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .That().ResideInNamespace($"{ns}.Application")
            .ShouldNot().HaveDependencyOnAny(
                $"{ns}.Infrastructure",
                $"{ns}.Endpoints",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Sgpla.BuildingBlocks.Infrastructure")
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Infrastructure_NoDependeDeEndpoints(string modulo)
    {
        var ns = Modulos.Namespace(modulo);

        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .That().ResideInNamespace($"{ns}.Infrastructure")
            .ShouldNot().HaveDependencyOn($"{ns}.Endpoints")
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Endpoints_NoDependeDeDomainNiInfrastructure(string modulo)
    {
        var ns = Modulos.Namespace(modulo);

        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .That().ResideInNamespace($"{ns}.Endpoints")
            .ShouldNot().HaveDependencyOnAny(
                $"{ns}.Domain",
                $"{ns}.Infrastructure",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Describir(resultado));
    }

    private static string Describir(NetArchTest.Rules.TestResult resultado) =>
        $"Tipos que incumplen la regla: {string.Join(", ", resultado.FailingTypeNames ?? [])}";
}
