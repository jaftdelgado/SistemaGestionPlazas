using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.UnitTests.OfertaEducativa.PlanesEstudio;

public sealed class ExperienciaEducativaTests
{
    [Fact]
    public void Crear_ConDatosValidos_CopiaLosDatosYNormalizaElNombre()
    {
        var resultado = ExperienciaEducativa.Crear(
            Datos() with { Nombre = "  Habilidades   de comunicación ", CupoMinimo = 5, CupoMaximo = 30, PerfilDocente = "Perfil" });

        resultado.IsSuccess.ShouldBeTrue();
        var experiencia = resultado.Value;
        experiencia.Nombre.ShouldBe("Habilidades de comunicación");
        experiencia.HorasTeoricas.ShouldBe(2);
        experiencia.HorasPracticas.ShouldBe(3);
        experiencia.Creditos.ShouldBe(6);
        experiencia.CupoMinimo.ShouldBe(5);
        experiencia.CupoMaximo.ShouldBe(30);
        experiencia.PerfilDocente.ShouldBe("Perfil");
        experiencia.AreaFormacionId.ShouldBe(1);
        experiencia.FechaEliminacion.ShouldBeNull();
    }

    [Fact]
    public void Crear_ConMateriaYCursoEnMinusculasYEspacios_LosPasaAMayusculasYConservaLosCerosIniciales()
    {
        var experiencia = ExperienciaEducativa.Crear(Datos() with { Materia = "  exav ", Curso = " 00001 " }).Value;

        experiencia.Materia.ShouldBe("EXAV");
        experiencia.Curso.ShouldBe("00001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_FallaConNombreVacio(string nombre)
    {
        ExperienciaEducativa.Crear(Datos() with { Nombre = nombre }).Error.ShouldBe(ExperienciaEducativaErrors.NombreVacio);
    }

    [Fact]
    public void Crear_ConNombreDeLongitudMaxima_LoAcepta()
    {
        ExperienciaEducativa.Crear(Datos() with { Nombre = new string('A', ExperienciaEducativa.LongitudMaximaNombre) })
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Crear_ConNombreDemasiadoLargo_FallaConNombreDemasiadoLargo()
    {
        ExperienciaEducativa.Crear(Datos() with { Nombre = new string('A', ExperienciaEducativa.LongitudMaximaNombre + 1) })
            .Error.ShouldBe(ExperienciaEducativaErrors.NombreDemasiadoLargo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Crear_SinMateria_FallaConMateriaVacia(string materia)
    {
        ExperienciaEducativa.Crear(Datos() with { Materia = materia }).Error.ShouldBe(ExperienciaEducativaErrors.MateriaVacia);
    }

    [Fact]
    public void Crear_ConMateriaDemasiadoLarga_FallaConMateriaDemasiadoLarga()
    {
        ExperienciaEducativa.Crear(Datos() with { Materia = new string('A', ExperienciaEducativa.LongitudMaximaMateria + 1) })
            .Error.ShouldBe(ExperienciaEducativaErrors.MateriaDemasiadoLarga);
    }

    [Theory]
    [InlineData("EN-SO")]
    [InlineData("EN SO")]
    [InlineData("ÉNSO")]
    public void Crear_ConMateriaDeFormatoInvalido_FallaConMateriaFormatoInvalido(string materia)
    {
        ExperienciaEducativa.Crear(Datos() with { Materia = materia })
            .Error.ShouldBe(ExperienciaEducativaErrors.MateriaFormatoInvalido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Crear_SinCurso_FallaConCursoVacio(string curso)
    {
        ExperienciaEducativa.Crear(Datos() with { Curso = curso }).Error.ShouldBe(ExperienciaEducativaErrors.CursoVacio);
    }

    [Fact]
    public void Crear_ConCursoDemasiadoLargo_FallaConCursoDemasiadoLargo()
    {
        ExperienciaEducativa.Crear(Datos() with { Curso = new string('1', ExperienciaEducativa.LongitudMaximaCurso + 1) })
            .Error.ShouldBe(ExperienciaEducativaErrors.CursoDemasiadoLargo);
    }

    [Theory]
    [InlineData("380-03")]
    [InlineData("38 003")]
    public void Crear_ConCursoDeFormatoInvalido_FallaConCursoFormatoInvalido(string curso)
    {
        ExperienciaEducativa.Crear(Datos() with { Curso = curso }).Error.ShouldBe(ExperienciaEducativaErrors.CursoFormatoInvalido);
    }

    [Fact]
    public void Crear_ConHorasTeoricasNegativas_FallaConHorasTeoricasNegativas()
    {
        ExperienciaEducativa.Crear(Datos() with { HorasTeoricas = -1 })
            .Error.ShouldBe(ExperienciaEducativaErrors.HorasTeoricasNegativas);
    }

    [Fact]
    public void Crear_ConHorasPracticasNegativas_FallaConHorasPracticasNegativas()
    {
        ExperienciaEducativa.Crear(Datos() with { HorasPracticas = -1 })
            .Error.ShouldBe(ExperienciaEducativaErrors.HorasPracticasNegativas);
    }

    [Fact]
    public void Crear_ConHorasEnCero_LasAcepta()
    {
        ExperienciaEducativa.Crear(Datos() with { HorasTeoricas = 0, HorasPracticas = 0 }).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Crear_ConCreditosNoPositivos_FallaConCreditosNoPositivos(int creditos)
    {
        ExperienciaEducativa.Crear(Datos() with { Creditos = creditos })
            .Error.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos);
    }

    [Fact]
    public void Crear_ConCupoMinimoNegativo_FallaConCupoMinimoNegativo()
    {
        ExperienciaEducativa.Crear(Datos() with { CupoMinimo = -1 }).Error.ShouldBe(ExperienciaEducativaErrors.CupoMinimoNegativo);
    }

    [Fact]
    public void Crear_ConCupoMaximoNegativo_FallaConCupoMaximoNegativo()
    {
        ExperienciaEducativa.Crear(Datos() with { CupoMaximo = -1 }).Error.ShouldBe(ExperienciaEducativaErrors.CupoMaximoNegativo);
    }

    [Fact]
    public void Crear_ConCuposInvertidos_FallaConCuposInvertidos()
    {
        ExperienciaEducativa.Crear(Datos() with { CupoMinimo = 30, CupoMaximo = 5 })
            .Error.ShouldBe(ExperienciaEducativaErrors.CuposInvertidos);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(0, 0)]
    [InlineData(5, null)]
    [InlineData(null, 5)]
    [InlineData(null, null)]
    public void Crear_ConCuposValidos_LosAcepta(int? minimo, int? maximo)
    {
        var experiencia = ExperienciaEducativa.Crear(Datos() with { CupoMinimo = minimo, CupoMaximo = maximo }).Value;

        experiencia.CupoMinimo.ShouldBe(minimo);
        experiencia.CupoMaximo.ShouldBe(maximo);
    }

    [Fact]
    public void Crear_ConVariosErrores_DevuelveElPrimeroEnElOrdenDeLaEspecificacion()
    {
        var datos = new DatosExperienciaEducativa("", "", "", -1, -1, 0, -1, -1, null, 1);

        ExperienciaEducativa.Crear(datos).Error.ShouldBe(ExperienciaEducativaErrors.NombreVacio);
        ExperienciaEducativa.Crear(datos with { Nombre = "X" }).Error.ShouldBe(ExperienciaEducativaErrors.MateriaVacia);
        ExperienciaEducativa.Crear(datos with { Nombre = "X", Materia = "A" }).Error.ShouldBe(ExperienciaEducativaErrors.CursoVacio);
        ExperienciaEducativa.Crear(datos with { Nombre = "X", Materia = "A", Curso = "1" })
            .Error.ShouldBe(ExperienciaEducativaErrors.HorasTeoricasNegativas);
        ExperienciaEducativa.Crear(datos with { Nombre = "X", Materia = "A", Curso = "1", HorasTeoricas = 0 })
            .Error.ShouldBe(ExperienciaEducativaErrors.HorasPracticasNegativas);
        ExperienciaEducativa.Crear(datos with { Nombre = "X", Materia = "A", Curso = "1", HorasTeoricas = 0, HorasPracticas = 0 })
            .Error.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \r\n ")]
    public void Crear_ConPerfilVacio_LoDejaNull(string? perfil)
    {
        ExperienciaEducativa.Crear(Datos() with { PerfilDocente = perfil }).Value.PerfilDocente.ShouldBeNull();
    }

    [Fact]
    public void Crear_ConPerfilConSaltosDeLinea_LosConservaYSoloRecortaLosExtremos()
    {
        var experiencia = ExperienciaEducativa.Crear(
            Datos() with { PerfilDocente = "  Licenciatura en:\n- Informática\r\n-   Sistemas  \n" }).Value;

        experiencia.PerfilDocente.ShouldBe("Licenciatura en:\n- Informática\r\n-   Sistemas");
    }

    [Fact]
    public void Errores_DeclaranSuCampo()
    {
        ExperienciaEducativaErrors.NombreVacio.Campo.ShouldBe("Nombre");
        ExperienciaEducativaErrors.NombreDemasiadoLargo.Campo.ShouldBe("Nombre");
        ExperienciaEducativaErrors.MateriaVacia.Campo.ShouldBe("Materia");
        ExperienciaEducativaErrors.MateriaDemasiadoLarga.Campo.ShouldBe("Materia");
        ExperienciaEducativaErrors.MateriaFormatoInvalido.Campo.ShouldBe("Materia");
        ExperienciaEducativaErrors.CursoVacio.Campo.ShouldBe("Curso");
        ExperienciaEducativaErrors.CursoDemasiadoLargo.Campo.ShouldBe("Curso");
        ExperienciaEducativaErrors.CursoFormatoInvalido.Campo.ShouldBe("Curso");
        ExperienciaEducativaErrors.HorasTeoricasNegativas.Campo.ShouldBe("HorasTeoricas");
        ExperienciaEducativaErrors.HorasPracticasNegativas.Campo.ShouldBe("HorasPracticas");
        ExperienciaEducativaErrors.CreditosNoPositivos.Campo.ShouldBe("Creditos");
        ExperienciaEducativaErrors.CupoMinimoNegativo.Campo.ShouldBe("CupoMinimo");
        ExperienciaEducativaErrors.CupoMaximoNegativo.Campo.ShouldBe("CupoMaximo");
        ExperienciaEducativaErrors.CuposInvertidos.Campo.ShouldBe("CupoMinimo");
        ExperienciaEducativaErrors.PlanEstudiosInexistente.Campo.ShouldBe("PlanEstudiosId");
        ExperienciaEducativaErrors.AreaFormacionInexistente.Campo.ShouldBe("AreaFormacionId");
    }

    [Fact]
    public void Modificar_SinProgramaciones_CambiaTodoLoEditable()
    {
        var experiencia = Experiencia();

        var resultado = experiencia.Modificar(
            "  Otro   nombre ", 4, 5, 9, 10, 40, "  Otro perfil ", areaFormacionId: 2, tuvoProgramaciones: false);

        resultado.IsSuccess.ShouldBeTrue();
        experiencia.Nombre.ShouldBe("Otro nombre");
        experiencia.HorasTeoricas.ShouldBe(4);
        experiencia.HorasPracticas.ShouldBe(5);
        experiencia.Creditos.ShouldBe(9);
        experiencia.CupoMinimo.ShouldBe(10);
        experiencia.CupoMaximo.ShouldBe(40);
        experiencia.PerfilDocente.ShouldBe("Otro perfil");
        experiencia.AreaFormacionId.ShouldBe(2);
        experiencia.Materia.ShouldBe("ENSO");
        experiencia.Curso.ShouldBe("38003");
    }

    [Fact]
    public void Modificar_ConProgramacionesYSoloNombrePerfilYCupos_LosCambia()
    {
        var experiencia = Experiencia();

        var resultado = experiencia.Modificar("Otro nombre", 2, 3, 6, 10, 40, "Otro perfil", 1, tuvoProgramaciones: true);

        resultado.IsSuccess.ShouldBeTrue();
        experiencia.Nombre.ShouldBe("Otro nombre");
        experiencia.CupoMinimo.ShouldBe(10);
        experiencia.CupoMaximo.ShouldBe(40);
        experiencia.PerfilDocente.ShouldBe("Otro perfil");
        experiencia.HorasTeoricas.ShouldBe(2);
        experiencia.HorasPracticas.ShouldBe(3);
        experiencia.Creditos.ShouldBe(6);
        experiencia.AreaFormacionId.ShouldBe(1);
    }

    [Theory]
    [InlineData(3, 3, 6, 1)]
    [InlineData(2, 4, 6, 1)]
    [InlineData(2, 3, 7, 1)]
    [InlineData(2, 3, 6, 2)]
    public void Modificar_ConProgramacionesYCambioCurricular_FallaConAtributosCurricularesInmutablesYNoCambiaNada(
        int horasTeoricas, int horasPracticas, int creditos, int areaFormacionId)
    {
        var experiencia = Experiencia();

        var resultado = experiencia.Modificar(
            "Otro nombre", horasTeoricas, horasPracticas, creditos, 10, 40, "Otro perfil", areaFormacionId, tuvoProgramaciones: true);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.AtributosCurricularesInmutables);
        experiencia.Nombre.ShouldBe("Habilidades de comunicación");
        experiencia.HorasTeoricas.ShouldBe(2);
        experiencia.HorasPracticas.ShouldBe(3);
        experiencia.Creditos.ShouldBe(6);
        experiencia.CupoMinimo.ShouldBe(5);
        experiencia.CupoMaximo.ShouldBe(30);
        experiencia.PerfilDocente.ShouldBe("Perfil");
        experiencia.AreaFormacionId.ShouldBe(1);
    }

    [Fact]
    public void Modificar_ConCuposYPerfilNull_LosQuita()
    {
        var experiencia = Experiencia();

        var resultado = experiencia.Modificar("Nombre", 2, 3, 6, null, null, null, 1, tuvoProgramaciones: true);

        resultado.IsSuccess.ShouldBeTrue();
        experiencia.CupoMinimo.ShouldBeNull();
        experiencia.CupoMaximo.ShouldBeNull();
        experiencia.PerfilDocente.ShouldBeNull();
    }

    [Fact]
    public void Modificar_ConCuposInvertidos_FallaYNoCambiaNada()
    {
        var experiencia = Experiencia();

        var resultado = experiencia.Modificar("Otro nombre", 4, 5, 9, 40, 10, "Otro perfil", 2, tuvoProgramaciones: false);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.CuposInvertidos);
        experiencia.Nombre.ShouldBe("Habilidades de comunicación");
        experiencia.HorasTeoricas.ShouldBe(2);
        experiencia.CupoMinimo.ShouldBe(5);
        experiencia.PerfilDocente.ShouldBe("Perfil");
        experiencia.AreaFormacionId.ShouldBe(1);
    }

    [Fact]
    public void Modificar_ConDatosInvalidosYProgramaciones_DevuelveElErrorDeValidacionAntesQueElDeInmutabilidad()
    {
        Experiencia().Modificar("   ", 9, 9, 9, null, null, null, 2, tuvoProgramaciones: true)
            .Error.ShouldBe(ExperienciaEducativaErrors.NombreVacio);
        Experiencia().Modificar("Nombre", 9, 9, 0, null, null, null, 2, tuvoProgramaciones: true)
            .Error.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos);
    }

    [Fact]
    public void DarDeBaja_EsIdempotente()
    {
        var experiencia = Experiencia();
        var primera = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        experiencia.DarDeBaja(primera);
        experiencia.DarDeBaja(primera.AddDays(1));

        experiencia.FechaEliminacion.ShouldBe(primera);
    }

    private static DatosExperienciaEducativa Datos() =>
        new("Habilidades de comunicación", "ENSO", "38003", 2, 3, 6, null, null, null, 1);

    private static ExperienciaEducativa Experiencia() =>
        ExperienciaEducativa.Crear(Datos() with { CupoMinimo = 5, CupoMaximo = 30, PerfilDocente = "Perfil" }).Value;
}
