using NetArchTest.Rules;

namespace Sgpla.ArchitectureTests;

public class DependenciasEntreModulosTests
{
    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Modulo_SoloDependeDeModulosPermitidos(string modulo)
    {
        var prohibidos = Modulos.Ensamblados.Keys
            .Where(otro => otro != modulo && !Modulos.DependenciasPermitidas[modulo].Contains(otro))
            .Select(Modulos.Namespace)
            .ToArray();

        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .ShouldNot()
            .HaveDependencyOnAny(prohibidos)
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(
            $"{modulo} depende de módulos no permitidos: {string.Join(", ", resultado.FailingTypeNames ?? [])}");
    }

    [Theory]
    [MemberData(nameof(Modulos.Nombres), MemberType = typeof(Modulos))]
    public void Modulo_SoloUsaLosContratosDeOtrosModulos(string modulo)
    {
        // Tipos de los módulos permitidos que quedan fuera de su contrato público.
        var internos = Modulos.DependenciasPermitidas[modulo]
            .SelectMany(otro => Types.InAssembly(Modulos.Ensamblados[otro])
                .That().DoNotResideInNamespace(Modulos.NamespaceContratos(otro))
                .GetTypes())
            .Where(tipo => !tipo.IsNested && tipo.FullName is not null && tipo.FullName.StartsWith("Sgpla.Modules.", StringComparison.Ordinal))
            .Select(tipo => tipo.FullName!)
            .ToArray();

        if (internos.Length == 0)
        {
            return;
        }

        var resultado = Types.InAssembly(Modulos.Ensamblados[modulo])
            .ShouldNot()
            .HaveDependencyOnAny(internos)
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(
            $"{modulo} usa tipos de otros módulos fuera de Application.Contracts: {string.Join(", ", resultado.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Grafo_NoTieneCiclos()
    {
        var visitados = new HashSet<string>();
        var enCurso = new HashSet<string>();

        bool TieneCiclo(string modulo)
        {
            if (enCurso.Contains(modulo))
            {
                return true;
            }

            if (!visitados.Add(modulo))
            {
                return false;
            }

            enCurso.Add(modulo);
            var ciclo = Modulos.DependenciasPermitidas[modulo].Any(TieneCiclo);
            enCurso.Remove(modulo);
            return ciclo;
        }

        Modulos.DependenciasPermitidas.Keys.Any(TieneCiclo).ShouldBeFalse();
    }
}
