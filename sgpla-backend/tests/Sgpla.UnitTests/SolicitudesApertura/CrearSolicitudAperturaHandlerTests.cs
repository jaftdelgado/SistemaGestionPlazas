using System.Text;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class CrearSolicitudAperturaHandlerTests
{
    private const int ExperienciaId = 301;
    private const int PeriodoActualId = 91;
    private const int PeriodoSiguienteId = 92;

    private readonly SolicitudAperturaRepositoryFalso _repositorio = new();
    private readonly ExperienciasEducativasFalsas _experiencias = new();
    private readonly PeriodosEscolaresFalsos _periodos = new();
    private readonly PeriodosConfiguradosFalsos _periodosConfigurados = new();
    private readonly AlmacenamientoArchivosFalso _almacenamiento = new();
    private readonly UsuarioActualFalso _actual = new(44, Rol.EntidadAcademica, 7);
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 10, 1, 15, 4, 5, 987, TimeSpan.Zero));

    public CrearSolicitudAperturaHandlerTests()
    {
        _experiencias.Resumenes.Add(ExperienciaId, new ExperienciaEducativaResumen(
            ExperienciaId,
            "ISOF",
            "00001",
            "Programación",
            10,
            40,
            true,
            7,
            "ISOF-14",
            5,
            "Ingeniería de Software",
            1,
            "Escolarizada",
            7,
            "FEI",
            "Facultad de Estadística e Informática",
            3));
        _periodos.PorId.Add(PeriodoActualId, Periodo(PeriodoActualId, _periodosConfigurados.ClaveActual));
        _periodos.PorId.Add(PeriodoSiguienteId, Periodo(PeriodoSiguienteId, _periodosConfigurados.ClaveSiguiente));
        _periodos.PorClave.Add(_periodosConfigurados.ClaveActual, _periodos.PorId[PeriodoActualId]);
        _periodos.PorClave.Add(_periodosConfigurados.ClaveSiguiente, _periodos.PorId[PeriodoSiguienteId]);
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_GuardaBinarioYSolicitudConLosDatosDerivados()
    {
        var comando = Comando();

        var resultado = await Handler().HandleAsync(comando, CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Estado.ShouldBe("PENDIENTE");
        resultado.Value.Seccion.ShouldBe("A2");
        resultado.Value.ExperienciaEducativa.Materia.ShouldBe("ISOF");
        resultado.Value.ExperienciaEducativa.Modalidad.ShouldBe("Escolarizada");
        resultado.Value.PeriodoEscolar.Clave.ShouldBe(_periodosConfigurados.ClaveSiguiente);
        resultado.Value.Oficio.Nombre.ShouldBe("oficio.pdf");
        resultado.Value.CreadaEn.ShouldBe(new DateTime(2026, 10, 1, 15, 4, 5, DateTimeKind.Utc));
        _repositorio.Agregadas.ShouldHaveSingleItem().CreadaPorUsuarioId.ShouldBe(44);
        _repositorio.Agregadas[0].Oficio.CargadoPorUsuarioId.ShouldBe(44);
        _almacenamiento.Guardados.ShouldBe(1);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConDatosDeDominioInvalidos_NoConsultaReferenciasNiGuardaArchivo()
    {
        var resultado = await Handler().HandleAsync(Comando() with { Seccion = "A 2" }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.SeccionFormatoInvalido);
        _experiencias.Consultas.ShouldBe(0);
        _almacenamiento.Guardados.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_SinOficio_FallaAntesDeConsultarLaExperiencia()
    {
        var resultado = await Handler().HandleAsync(Comando() with { Oficio = null }, CancellationToken.None);

        resultado.Error.ShouldBe(ArchivoSolicitudAperturaErrors.Obligatorio);
        _experiencias.Consultas.ShouldBe(0);
        _almacenamiento.Guardados.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConFirmaPdfInvalida_FallaAntesDeConsultarLaExperiencia()
    {
        var resultado = await Handler().HandleAsync(Comando(Encoding.ASCII.GetBytes("texto")), CancellationToken.None);

        resultado.Error.ShouldBe(ArchivoSolicitudAperturaErrors.NoEsPdf);
        _experiencias.Consultas.ShouldBe(0);
        _almacenamiento.Guardados.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConExperienciaInexistente_FallaEnExperienciaEducativaId()
    {
        _experiencias.Resumenes.Clear();

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.ExperienciaEducativaInvalida);
        resultado.Error.Campo.ShouldBe("ExperienciaEducativaId");
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 8)]
    public async Task HandleAsync_ConExperienciaNoVigenteODeOtraEntidad_FallaEnExperienciaEducativaId(
        bool vigente,
        int entidadId)
    {
        _experiencias.Resumenes[ExperienciaId] = _experiencias.Resumenes[ExperienciaId] with
        {
            Vigente = vigente,
            EntidadAcademicaId = entidadId,
        };

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.ExperienciaEducativaInvalida);
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPeriodoInexistente_FallaEnPeriodoEscolarId()
    {
        var resultado = await Handler().HandleAsync(Comando() with { PeriodoEscolarId = 999 }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.PeriodoEscolarInvalido);
        resultado.Error.Campo.ShouldBe("PeriodoEscolarId");
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPeriodoConfiguradoInactivo_FallaConPeriodosNoDisponibles()
    {
        _periodos.PorClave.Remove(_periodosConfigurados.ClaveActual);

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.PeriodosNoDisponibles);
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPeriodoValidoQueNoEsElSiguiente_FallaConPeriodoNoAbierto()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { PeriodoEscolarId = PeriodoActualId }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.PeriodoNoAbierto);
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(41)]
    public async Task HandleAsync_FueraDeCupos_FallaAntesDeGuardarArchivo(int cantidad)
    {
        var resultado = await Handler().HandleAsync(Comando() with { CantidadEstudiantes = cantidad }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.CantidadFueraDeCupos);
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudPendienteDeLaSeccion_FallaSinGuardarArchivo()
    {
        _repositorio.TienePendiente = true;

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.SeccionDuplicada);
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_SiGuardarBaseFalla_EliminaElBinarioYRelanzaLaMismaExcepcion()
    {
        var excepcion = new InvalidOperationException("Error de prueba.");
        _unidadDeTrabajo.Excepcion = excepcion;

        var lanzada = await Should.ThrowAsync<InvalidOperationException>(() =>
            Handler().HandleAsync(Comando(), CancellationToken.None));

        lanzada.ShouldBeSameAs(excepcion);
        _almacenamiento.Guardados.ShouldBe(1);
        _almacenamiento.Eliminados.ShouldHaveSingleItem().ShouldStartWith("solicitudes-apertura/");
    }

    private static CrearSolicitudAperturaCommand Comando(byte[]? bytes = null) => new(
        ExperienciaId,
        PeriodoSiguienteId,
        " a2 ",
        25,
        "Justificación válida.",
        new ArchivoRecibido(
            "C:\\fakepath\\oficio.pdf",
            "application/pdf",
            (bytes ?? Pdf).LongLength,
            () => new MemoryStream(bytes ?? Pdf, writable: false)));

    private CrearSolicitudAperturaHandler Handler() => new(
        _repositorio,
        _experiencias,
        _periodos,
        _periodosConfigurados,
        _almacenamiento,
        _actual,
        _unidadDeTrabajo,
        _reloj);

    private static PeriodoEscolarResumen Periodo(int id, string clave) =>
        new(id, clave, new DateOnly(2026, 8, 10), new DateOnly(2027, 1, 22), true);

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7");
}
