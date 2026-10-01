using System.Text;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class ModificarSolicitudAperturaHandlerTests
{
    private readonly SolicitudAperturaRepositoryFalso _repositorio = new();
    private readonly ExperienciasEducativasFalsas _experiencias = new();
    private readonly AlmacenamientoArchivosFalso _almacenamiento = new();
    private readonly UsuarioActualFalso _actual = new(44, Rol.EntidadAcademica, DatosDePrueba.EntidadId);
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 10, 2, 9, 30, 0, 987, TimeSpan.Zero));
    private readonly SolicitudApertura _solicitud = DatosDePrueba.Solicitud();

    public ModificarSolicitudAperturaHandlerTests()
    {
        _experiencias.Resumenes.Add(DatosDePrueba.ExperienciaId, DatosDePrueba.Experiencia());
        _repositorio.Precargar(DatosDePrueba.SolicitudId, _solicitud);
    }

    [Fact]
    public async Task HandleAsync_ConIdInexistente_DevuelveNoEncontrada()
    {
        var resultado = await Handler().HandleAsync(Comando() with { Id = 999 }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(999));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudDeOtraEntidad_DevuelveNoEncontrada()
    {
        _experiencias.Resumenes[DatosDePrueba.ExperienciaId] = DatosDePrueba.Experiencia(entidadId: 8);

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(DatosDePrueba.SolicitudId));
        _solicitud.CantidadEstudiantes.ShouldBe(25);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_SinOficio_ModificaYConservaElActualSinGuardarBinario()
    {
        var oficioActual = _solicitud.Oficio;

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        _solicitud.CantidadEstudiantes.ShouldBe(30);
        _solicitud.Justificacion.ShouldBe("Justificación nueva.");
        _solicitud.ActualizadaEn.ShouldBe(new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc));
        _solicitud.ActualizadaPorUsuarioId.ShouldBe(44);
        _solicitud.Oficio.ShouldBeSameAs(oficioActual);
        _almacenamiento.Guardados.ShouldBe(0);
        _almacenamiento.Eliminados.ShouldBeEmpty();
        _repositorio.OficiosEliminados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Theory]
    [InlineData("text/plain", "%PDF-1.7")]
    [InlineData("application/pdf", "texto")]
    public async Task HandleAsync_ConOficioInvalido_FallaSinGuardarBinarioNiModificar(string tipo, string contenido)
    {
        var resultado = await Handler().HandleAsync(
            Comando(Oficio(Encoding.ASCII.GetBytes(contenido), tipo)), CancellationToken.None);

        resultado.Error.ShouldBe(ArchivoSolicitudAperturaErrors.NoEsPdf);
        resultado.Error.Campo.ShouldBe("Oficio");
        _almacenamiento.Guardados.ShouldBe(0);
        _repositorio.OficiosEliminados.ShouldBeEmpty();
        _solicitud.CantidadEstudiantes.ShouldBe(25);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConErrorDeDominioYOficioValido_NoGuardaElBinario()
    {
        var resultado = await Handler().HandleAsync(
            Comando(Oficio(Pdf)) with { CantidadEstudiantes = 0 }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.CantidadNoPositiva);
        _almacenamiento.Guardados.ShouldBe(0);
        _repositorio.OficiosEliminados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudNoPendiente_DevuelveNoPendienteSinGuardarBinario()
    {
        _solicitud.Cancelar("Ya no se necesita.", 44, DatosDePrueba.Fecha).IsSuccess.ShouldBeTrue();

        var resultado = await Handler().HandleAsync(Comando(Oficio(Pdf)), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoPendiente);
        _almacenamiento.Guardados.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_FueraDeCupos_DevuelveCantidadFueraDeCuposSinGuardarBinario()
    {
        var resultado = await Handler().HandleAsync(
            Comando(Oficio(Pdf)) with { CantidadEstudiantes = 41 }, CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.CantidadFueraDeCupos);
        _almacenamiento.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConOficio_ReemplazaElAnteriorYLoEliminaSoloDespuesDeConfirmar()
    {
        var anterior = _solicitud.Oficio;
        int? eliminadosAlGuardar = null;
        _unidadDeTrabajo.AlGuardar = () => eliminadosAlGuardar = _almacenamiento.Eliminados.Count;

        var resultado = await Handler().HandleAsync(Comando(Oficio(Pdf)), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        _repositorio.OficiosEliminados.ShouldHaveSingleItem().ShouldBeSameAs(anterior);
        _solicitud.Oficio.ShouldNotBeSameAs(anterior);
        _solicitud.Oficio.Nombre.ShouldBe("nuevo.pdf");
        _solicitud.Oficio.CargadoPorUsuarioId.ShouldBe(44);
        _almacenamiento.Guardados.ShouldBe(1);
        eliminadosAlGuardar.ShouldBe(0);
        _almacenamiento.Eliminados.ShouldHaveSingleItem().ShouldBe(DatosDePrueba.ClaveOficioOriginal);
    }

    [Fact]
    public async Task HandleAsync_ConOficioSiGuardarBaseFalla_EliminaElBinarioNuevoNoElAnteriorYRelanzaLaMismaExcepcion()
    {
        var excepcion = new InvalidOperationException("Error de prueba.");
        _unidadDeTrabajo.Excepcion = excepcion;

        var lanzada = await Should.ThrowAsync<InvalidOperationException>(() =>
            Handler().HandleAsync(Comando(Oficio(Pdf)), CancellationToken.None));

        lanzada.ShouldBeSameAs(excepcion);
        _almacenamiento.Guardados.ShouldBe(1);
        var eliminado = _almacenamiento.Eliminados.ShouldHaveSingleItem();
        eliminado.ShouldStartWith("solicitudes-apertura/");
        eliminado.ShouldNotBe(DatosDePrueba.ClaveOficioOriginal);
    }

    [Fact]
    public async Task HandleAsync_SinOficioSiGuardarBaseFalla_NoEliminaNingunBinarioYRelanzaLaMismaExcepcion()
    {
        var excepcion = new InvalidOperationException("Error de prueba.");
        _unidadDeTrabajo.Excepcion = excepcion;

        var lanzada = await Should.ThrowAsync<InvalidOperationException>(() =>
            Handler().HandleAsync(Comando(), CancellationToken.None));

        lanzada.ShouldBeSameAs(excepcion);
        _almacenamiento.Eliminados.ShouldBeEmpty();
    }

    private static ModificarSolicitudAperturaCommand Comando(ArchivoRecibido? oficio = null) =>
        new(DatosDePrueba.SolicitudId, 30, "  Justificación nueva.  ", oficio);

    private static ArchivoRecibido Oficio(byte[] bytes, string tipo = "application/pdf") =>
        new("C:\\fakepath\\nuevo.pdf", tipo, bytes.LongLength, () => new MemoryStream(bytes, writable: false));

    private ModificarSolicitudAperturaHandler Handler() => new(
        _repositorio,
        _experiencias,
        _almacenamiento,
        _actual,
        _unidadDeTrabajo,
        _reloj);

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7");
}
