using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

namespace Sgpla.UnitTests.OfertaEducativa.PeriodosEscolares;

public sealed class PeriodoEscolarTests
{
    private static readonly DateOnly Inicio = new(2026, 8, 10);
    private static readonly DateOnly Fin = new(2027, 1, 22);

    [Fact]
    public void Crear_ConDatosValidos_CreaElPeriodo()
    {
        var resultado = PeriodoEscolar.Crear("202701", Inicio, Fin);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Clave.ShouldBe("202701");
        resultado.Value.FechaInicio.ShouldBe(Inicio);
        resultado.Value.FechaFin.ShouldBe(Fin);
        resultado.Value.FechaEliminacion.ShouldBeNull();
    }

    [Fact]
    public void Crear_ConEspacios_RecortaLaClave()
    {
        PeriodoEscolar.Crear("  202701 ", Inicio, Fin).Value.Clave.ShouldBe("202701");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinClave_FallaConClaveVacia(string clave)
    {
        PeriodoEscolar.Crear(clave, Inicio, Fin).Error.ShouldBe(PeriodoEscolarErrors.ClaveVacia);
    }

    [Theory]
    [InlineData("20270")]
    [InlineData("2027011")]
    [InlineData("2027A1")]
    [InlineData("20 701")]
    [InlineData("٢٠٢٧٠١")]
    public void Crear_ConClaveQueNoSonSeisDigitos_FallaConClaveFormatoInvalido(string clave)
    {
        PeriodoEscolar.Crear(clave, Inicio, Fin).Error.ShouldBe(PeriodoEscolarErrors.ClaveFormatoInvalido);
    }

    [Fact]
    public void Crear_ConFechaFinAnteriorAlInicio_FallaConRangoFechasInvalido()
    {
        PeriodoEscolar.Crear("202701", Fin, Inicio).Error.ShouldBe(PeriodoEscolarErrors.RangoFechasInvalido);
    }

    [Fact]
    public void Crear_ConFechasIguales_LoAcepta()
    {
        PeriodoEscolar.Crear("202701", Inicio, Inicio).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Crear_ConClaveYRangoInvalidos_DevuelveElErrorDeLaClave()
    {
        PeriodoEscolar.Crear("", Fin, Inicio).Error.ShouldBe(PeriodoEscolarErrors.ClaveVacia);
    }

    [Fact]
    public void Errores_DeclaranSuCampo()
    {
        PeriodoEscolarErrors.ClaveVacia.Campo.ShouldBe("Clave");
        PeriodoEscolarErrors.ClaveFormatoInvalido.Campo.ShouldBe("Clave");
        PeriodoEscolarErrors.RangoFechasInvalido.Campo.ShouldBe("FechaFin");
    }

    [Fact]
    public void Modificar_ConRangoValido_CambiaLasFechasYConservaLaClave()
    {
        var periodo = PeriodoEscolar.Crear("202701", Inicio, Fin).Value;
        var nuevoInicio = new DateOnly(2026, 8, 17);
        var nuevoFin = new DateOnly(2027, 2, 5);

        var resultado = periodo.Modificar(nuevoInicio, nuevoFin);

        resultado.IsSuccess.ShouldBeTrue();
        periodo.FechaInicio.ShouldBe(nuevoInicio);
        periodo.FechaFin.ShouldBe(nuevoFin);
        periodo.Clave.ShouldBe("202701");
    }

    [Fact]
    public void Modificar_ConRangoInvalido_FallaYNoCambiaNada()
    {
        var periodo = PeriodoEscolar.Crear("202701", Inicio, Fin).Value;

        var resultado = periodo.Modificar(Fin, Inicio);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.RangoFechasInvalido);
        periodo.FechaInicio.ShouldBe(Inicio);
        periodo.FechaFin.ShouldBe(Fin);
    }

    [Fact]
    public void DarDeBaja_EsIdempotente()
    {
        var periodo = PeriodoEscolar.Crear("202701", Inicio, Fin).Value;
        var primera = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        periodo.DarDeBaja(primera);
        periodo.DarDeBaja(primera.AddDays(1));

        periodo.FechaEliminacion.ShouldBe(primera);
    }
}
