using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.UnitTests.OfertaEducativa.PlanesEstudio;

public sealed class PlanEstudiosTests
{
    private const int ProgramaId = 12;

    [Fact]
    public void Crear_ConCodigoEnMinusculasYEspacios_LoNormalizaConGuiones()
    {
        var resultado = PlanEstudios.Crear("  isof-14-e-cr ", ProgramaId, [Datos()]);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Codigo.ShouldBe("ISOF-14-E-CR");
        resultado.Value.ProgramaEducativoId.ShouldBe(ProgramaId);
        resultado.Value.FechaEliminacion.ShouldBeNull();
        resultado.Value.ExperienciasEducativas.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinCodigo_FallaConCodigoVacio(string codigo)
    {
        PlanEstudios.Crear(codigo, ProgramaId, [Datos()]).Error.ShouldBe(PlanEstudiosErrors.CodigoVacio);
    }

    [Fact]
    public void Crear_ConCodigoDeLongitudMaxima_LoAcepta()
    {
        PlanEstudios.Crear(new string('A', PlanEstudios.LongitudMaximaCodigo), ProgramaId, [Datos()])
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Crear_ConCodigoDemasiadoLargo_FallaConCodigoDemasiadoLargo()
    {
        PlanEstudios.Crear(new string('A', PlanEstudios.LongitudMaximaCodigo + 1), ProgramaId, [Datos()])
            .Error.ShouldBe(PlanEstudiosErrors.CodigoDemasiadoLargo);
    }

    [Theory]
    [InlineData("ISOF_14")]
    [InlineData("ISOF 14")]
    [InlineData("ÁRBOL")]
    [InlineData("ISOF.14")]
    public void Crear_ConCodigoDeFormatoInvalido_FallaConCodigoFormatoInvalido(string codigo)
    {
        PlanEstudios.Crear(codigo, ProgramaId, [Datos()]).Error.ShouldBe(PlanEstudiosErrors.CodigoFormatoInvalido);
    }

    [Fact]
    public void Crear_ConListaVacia_FallaConSinExperiencias()
    {
        PlanEstudios.Crear("ISOF-14", ProgramaId, []).Error.ShouldBe(PlanEstudiosErrors.SinExperiencias);
    }

    [Fact]
    public void Crear_ConCodigoInvalidoYListaVacia_DevuelveElErrorDelCodigo()
    {
        PlanEstudios.Crear("", ProgramaId, []).Error.ShouldBe(PlanEstudiosErrors.CodigoVacio);
    }

    [Fact]
    public void Crear_ConTrescientasExperiencias_LasAcepta()
    {
        PlanEstudios.Crear("ISOF-14", ProgramaId, Experiencias(PlanEstudios.MaximoExperienciasImportacion))
            .Value.ExperienciasEducativas.Count.ShouldBe(300);
    }

    [Fact]
    public void Crear_ConTrescientasUnaExperiencias_FallaConDemasiadasExperiencias()
    {
        PlanEstudios.Crear("ISOF-14", ProgramaId, Experiencias(PlanEstudios.MaximoExperienciasImportacion + 1))
            .Error.ShouldBe(PlanEstudiosErrors.DemasiadasExperiencias);
    }

    [Fact]
    public void Crear_ConUnaExperienciaInvalida_FallaConElIndiceEnElCampo()
    {
        var experiencias = Experiencias(5);
        experiencias[2] = experiencias[2] with { Creditos = 0 };

        var error = PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).Error;

        error.Code.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos.Code);
        error.Type.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos.Type);
        error.Message.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos.Message);
        error.Campo.ShouldBe("ExperienciasEducativas[2].Creditos");
    }

    [Fact]
    public void Crear_ConVariasExperienciasInvalidas_DevuelveElPrimerError()
    {
        var experiencias = Experiencias(5);
        experiencias[1] = experiencias[1] with { Nombre = "  " };
        experiencias[3] = experiencias[3] with { Creditos = 0 };

        var error = PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).Error;

        error.Code.ShouldBe(ExperienciaEducativaErrors.NombreVacio.Code);
        error.Campo.ShouldBe("ExperienciasEducativas[1].Nombre");
    }

    [Fact]
    public void Crear_ConMateriaYCursoRepetidosTrasNormalizar_FallaConExperienciaRepetidaEnLaSegunda()
    {
        var experiencias = new List<DatosExperienciaEducativa>
        {
            Datos(materia: "FBGR", curso: "80001"),
            Datos(materia: " enso ", curso: "38003"),
            Datos(materia: "ENSO", curso: "38003"),
        };

        var error = PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).Error;

        error.ShouldBe(PlanEstudiosErrors.ExperienciaRepetida(2));
        error.Campo.ShouldBe("ExperienciasEducativas[2].Curso");
    }

    [Fact]
    public void Crear_ConUnElementoNulo_FallaConExperienciaVaciaEnSuIndice()
    {
        var experiencias = new List<DatosExperienciaEducativa?>(Experiencias(4)) { [2] = null };

        var error = PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).Error;

        error.ShouldBe(PlanEstudiosErrors.ExperienciaVacia(2));
        error.Campo.ShouldBe("ExperienciasEducativas[2]");
    }

    [Fact]
    public void Crear_ConUnaExperienciaInvalidaAntesDeUnNulo_DevuelveElErrorDeLaInvalida()
    {
        var experiencias = new List<DatosExperienciaEducativa?>(Experiencias(3)) { [2] = null };
        experiencias[0] = Datos() with { Creditos = 0 };

        PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).Error.Campo.ShouldBe("ExperienciasEducativas[0].Creditos");
    }

    [Fact]
    public void Crear_ConUnaExperienciaInvalidaYOtraRepetida_DevuelveElErrorDeLaExperienciaInvalida()
    {
        var experiencias = new List<DatosExperienciaEducativa>
        {
            Datos(materia: "ENSO", curso: "38003"),
            Datos(materia: "ENSO", curso: "38003"),
            Datos(materia: "ENSO", curso: "38004") with { Creditos = 0 },
        };

        PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).Error.Campo.ShouldBe("ExperienciasEducativas[2].Creditos");
    }

    [Fact]
    public void Crear_ConLaMismaMateriaYOtroCurso_LasAcepta()
    {
        var experiencias = new List<DatosExperienciaEducativa>
        {
            Datos(materia: "ENSO", curso: "38003"),
            Datos(materia: "ENSO", curso: "38004"),
        };

        PlanEstudios.Crear("ISOF-14", ProgramaId, experiencias).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AgregarExperiencia_ConDatosValidos_LaAgregaAlPlan()
    {
        var plan = PlanEstudios.Crear("ISOF-14", ProgramaId, [Datos()]).Value;

        var resultado = plan.AgregarExperiencia(Datos(materia: " fbgr ", curso: "80001"));

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Materia.ShouldBe("FBGR");
        plan.ExperienciasEducativas.Count.ShouldBe(2);
        plan.ExperienciasEducativas.ShouldContain(resultado.Value);
    }

    [Fact]
    public void AgregarExperiencia_ConDatosInvalidos_FallaSinAgregarNada()
    {
        var plan = PlanEstudios.Crear("ISOF-14", ProgramaId, [Datos()]).Value;

        var resultado = plan.AgregarExperiencia(Datos() with { Creditos = 0 });

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos);
        plan.ExperienciasEducativas.Count.ShouldBe(1);
    }

    [Fact]
    public void DarDeBaja_MarcaElPlanYSusExperienciasConElMismoInstante()
    {
        var plan = PlanEstudios.Crear("ISOF-14", ProgramaId, Experiencias(3)).Value;
        var instante = new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc);

        plan.DarDeBaja(instante);

        plan.FechaEliminacion.ShouldBe(instante);
        plan.ExperienciasEducativas.ShouldAllBe(e => e.FechaEliminacion == instante);
    }

    [Fact]
    public void DarDeBaja_EsIdempotente()
    {
        var plan = PlanEstudios.Crear("ISOF-14", ProgramaId, Experiencias(2)).Value;
        var primera = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        plan.DarDeBaja(primera);
        plan.DarDeBaja(primera.AddDays(1));

        plan.FechaEliminacion.ShouldBe(primera);
        plan.ExperienciasEducativas.ShouldAllBe(e => e.FechaEliminacion == primera);
    }

    [Fact]
    public void Errores_DeclaranSuCampo()
    {
        PlanEstudiosErrors.CodigoVacio.Campo.ShouldBe("Codigo");
        PlanEstudiosErrors.CodigoDemasiadoLargo.Campo.ShouldBe("Codigo");
        PlanEstudiosErrors.CodigoFormatoInvalido.Campo.ShouldBe("Codigo");
        PlanEstudiosErrors.SinExperiencias.Campo.ShouldBe("ExperienciasEducativas");
        PlanEstudiosErrors.DemasiadasExperiencias.Campo.ShouldBe("ExperienciasEducativas");
        PlanEstudiosErrors.ProgramaEducativoInexistente.Campo.ShouldBe("ProgramaEducativoId");
        PlanEstudiosErrors.ExperienciaRepetida(4).Campo.ShouldBe("ExperienciasEducativas[4].Curso");
        PlanEstudiosErrors.AreaFormacionInexistente(7).Campo.ShouldBe("ExperienciasEducativas[7].AreaFormacionId");
    }

    private static DatosExperienciaEducativa Datos(string materia = "ENSO", string curso = "38003") =>
        new("Habilidades de comunicación", materia, curso, 2, 2, 6, null, null, null, 1);

    /// <summary>Experiencias válidas con cursos distintos entre sí.</summary>
    private static List<DatosExperienciaEducativa> Experiencias(int cantidad) =>
        Enumerable.Range(0, cantidad).Select(i => Datos(curso: $"{i:D5}")).ToList();
}
