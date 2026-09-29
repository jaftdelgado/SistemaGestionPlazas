using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.UnitTests.OfertaEducativa.ProgramasEducativos;

public sealed class ProgramaEducativoTests
{
    private const int EntidadId = 7;
    private const int SistemaId = 1;
    private const int NivelId = 3;

    [Fact]
    public void Crear_ConEspaciosRepetidos_NormalizaElNombre()
    {
        var resultado = ProgramaEducativo.Crear("  Ingeniería   de  Software ", EntidadId, SistemaId, NivelId);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Nombre.ShouldBe("Ingeniería de Software");
        resultado.Value.EntidadAcademicaId.ShouldBe(EntidadId);
        resultado.Value.SistemaEducativoId.ShouldBe(SistemaId);
        resultado.Value.NivelFormacionId.ShouldBe(NivelId);
        resultado.Value.FechaEliminacion.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_FallaConNombreVacio(string nombre)
    {
        ProgramaEducativo.Crear(nombre, EntidadId, SistemaId, NivelId).Error.ShouldBe(ProgramaEducativoErrors.NombreVacio);
    }

    [Fact]
    public void Crear_ConNombreDeLongitudMaxima_LoAcepta()
    {
        ProgramaEducativo.Crear(new string('A', ProgramaEducativo.LongitudMaximaNombre), EntidadId, SistemaId, NivelId)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Crear_ConNombreDemasiadoLargo_FallaConNombreDemasiadoLargo()
    {
        ProgramaEducativo.Crear(new string('A', ProgramaEducativo.LongitudMaximaNombre + 1), EntidadId, SistemaId, NivelId)
            .Error.ShouldBe(ProgramaEducativoErrors.NombreDemasiadoLargo);
    }

    [Fact]
    public void Errores_DeclaranSuCampo()
    {
        ProgramaEducativoErrors.NombreVacio.Campo.ShouldBe("Nombre");
        ProgramaEducativoErrors.NombreDemasiadoLargo.Campo.ShouldBe("Nombre");
        ProgramaEducativoErrors.EntidadAcademicaInexistente.Campo.ShouldBe("EntidadAcademicaId");
        ProgramaEducativoErrors.SistemaEducativoInexistente.Campo.ShouldBe("SistemaEducativoId");
        ProgramaEducativoErrors.NivelFormacionInexistente.Campo.ShouldBe("NivelFormacionId");
    }

    [Fact]
    public void Modificar_SinPlanes_CambiaNombreSistemaYNivel()
    {
        var programa = Programa();

        var resultado = programa.Modificar("  Ingeniería   de Software ", 2, 4, tuvoPlanes: false);

        resultado.IsSuccess.ShouldBeTrue();
        programa.Nombre.ShouldBe("Ingeniería de Software");
        programa.SistemaEducativoId.ShouldBe(2);
        programa.NivelFormacionId.ShouldBe(4);
        programa.EntidadAcademicaId.ShouldBe(EntidadId);
    }

    [Fact]
    public void Modificar_ConPlanesYSoloElNombre_CambiaElNombre()
    {
        var programa = Programa();

        var resultado = programa.Modificar("Otro nombre", SistemaId, NivelId, tuvoPlanes: true);

        resultado.IsSuccess.ShouldBeTrue();
        programa.Nombre.ShouldBe("Otro nombre");
    }

    [Theory]
    [InlineData(2, NivelId)]
    [InlineData(SistemaId, 4)]
    [InlineData(2, 4)]
    public void Modificar_ConPlanesYCambioDeClasificacion_FallaConClasificacionInmutableYNoCambiaNada(int sistemaId, int nivelId)
    {
        var programa = Programa();

        var resultado = programa.Modificar("Otro nombre", sistemaId, nivelId, tuvoPlanes: true);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.ClasificacionInmutable);
        programa.Nombre.ShouldBe("Ingeniería de Software");
        programa.SistemaEducativoId.ShouldBe(SistemaId);
        programa.NivelFormacionId.ShouldBe(NivelId);
    }

    [Fact]
    public void Modificar_ConNombreInvalido_FallaYNoCambiaNada()
    {
        var programa = Programa();

        var resultado = programa.Modificar("   ", 2, 4, tuvoPlanes: false);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NombreVacio);
        programa.Nombre.ShouldBe("Ingeniería de Software");
        programa.SistemaEducativoId.ShouldBe(SistemaId);
        programa.NivelFormacionId.ShouldBe(NivelId);
    }

    [Fact]
    public void Modificar_ConNombreInvalidoYPlanes_DevuelveElErrorDelNombre()
    {
        Programa().Modificar("", 2, 4, tuvoPlanes: true).Error.ShouldBe(ProgramaEducativoErrors.NombreVacio);
    }

    [Fact]
    public void DarDeBaja_EsIdempotente()
    {
        var programa = Programa();
        var primera = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        programa.DarDeBaja(primera);
        programa.DarDeBaja(primera.AddDays(1));

        programa.FechaEliminacion.ShouldBe(primera);
    }

    private static ProgramaEducativo Programa() =>
        ProgramaEducativo.Crear("Ingeniería de Software", EntidadId, SistemaId, NivelId).Value;
}
