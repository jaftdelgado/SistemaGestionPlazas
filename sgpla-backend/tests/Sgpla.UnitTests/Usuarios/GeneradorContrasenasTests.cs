using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

namespace Sgpla.UnitTests.Usuarios;

public sealed class GeneradorContrasenasTests
{
    private const string CaracteresAmbiguos = "IOl01";

    [Fact]
    public void GenerarTemporal_MilVeces_CumpleLaPoliticaYSonDistintas()
    {
        var generador = new GeneradorContrasenas();
        var temporales = Enumerable.Range(0, 1000).Select(_ => generador.GenerarTemporal()).ToList();

        foreach (var temporal in temporales)
        {
            temporal.Length.ShouldBe(12);
            PoliticaContrasena.Validar(temporal).IsSuccess.ShouldBeTrue();
            temporal.Any(caracter => CaracteresAmbiguos.Contains(caracter)).ShouldBeFalse();
        }

        temporales.Distinct(StringComparer.Ordinal).Count().ShouldBe(temporales.Count);
    }
}
