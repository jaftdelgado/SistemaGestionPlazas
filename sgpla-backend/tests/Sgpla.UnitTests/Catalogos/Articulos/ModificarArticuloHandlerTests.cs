using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.UnitTests.Catalogos.Articulos;

public sealed class ModificarArticuloHandlerTests
{
    private const int Id = 7;
    private const string Descripcion = "Descripción original.";

    private readonly ArticuloRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly Articulo _articulo = Articulo.Crear("42", Descripcion).Value;

    public ModificarArticuloHandlerTests()
    {
        _repositorio.Registrar(Id, _articulo);
    }

    [Fact]
    public async Task HandleAsync_SinReferencias_ModificaNumeroYDescripcion()
    {
        var resultado = await Handler(tieneReferencias: false).HandleAsync(
            Comando("42 bis", "Nueva descripción."), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _articulo.Numero.ShouldBe("42 BIS");
        _articulo.Descripcion.ShouldBe("Nueva descripción.");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciasYOtroNumero_FallaConNumeroInmutableSinGuardar()
    {
        var resultado = await Handler(tieneReferencias: true).HandleAsync(
            Comando("43", descripcion: null), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ArticuloErrors.NumeroInmutable);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciasYSoloOtraDescripcion_ModificaSinConsultarReferencias()
    {
        var referencias = new ReferenciasArticuloFalsas(tieneReferencias: true);

        var resultado = await new ModificarArticuloHandler(_repositorio, [referencias], _unidadDeTrabajo).HandleAsync(
            Comando(" 42 ", "Nueva descripción."), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _articulo.Descripcion.ShouldBe("Nueva descripción.");
        referencias.Consultas.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConDescripcionNula_ConservaLaActual()
    {
        var resultado = await Handler(tieneReferencias: false).HandleAsync(
            Comando("43", descripcion: null), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _articulo.Numero.ShouldBe("43");
        _articulo.Descripcion.ShouldBe(Descripcion);
    }

    [Fact]
    public async Task HandleAsync_SinCambios_TerminaSinGuardar()
    {
        var resultado = await Handler(tieneReferencias: true).HandleAsync(
            Comando("42", Descripcion), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConNumeroDeOtroArticulo_FallaConNumeroDuplicadoSinGuardar()
    {
        _repositorio.NumerosExistentes.Add("43");

        var resultado = await Handler(tieneReferencias: false).HandleAsync(
            Comando("43", descripcion: null), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ArticuloErrors.NumeroDuplicado);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConArticuloInexistente_FallaConNoEncontrado()
    {
        var resultado = await Handler(tieneReferencias: false).HandleAsync(
            new ModificarArticuloCommand(Id + 1, "43", Descripcion: null), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ArticuloErrors.NoEncontrado(Id + 1));
    }

    private static ModificarArticuloCommand Comando(string numero, string? descripcion) => new(Id, numero, descripcion);

    private ModificarArticuloHandler Handler(bool tieneReferencias) =>
        new(_repositorio, [new ReferenciasArticuloFalsas(tieneReferencias)], _unidadDeTrabajo);
}
