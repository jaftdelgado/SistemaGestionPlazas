using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.UnitTests.Catalogos.Articulos;

public sealed class CrearArticuloHandlerTests
{
    private readonly ArticuloRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_ConNumeroNuevo_AgregaYGuardaElNumeroNormalizado()
    {
        var resultado = await Handler().HandleAsync(
            new CrearArticuloCommand(" 42  bis ", "Descripción."), TestContext.Current.CancellationToken);

        resultado.Value.Numero.ShouldBe("42 BIS");
        _repositorio.Agregados.ShouldHaveSingleItem().Numero.ShouldBe("42 BIS");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConNumeroExistente_FallaConNumeroDuplicadoSinGuardar()
    {
        _repositorio.NumerosExistentes.Add("42 BIS");

        var resultado = await Handler().HandleAsync(
            new CrearArticuloCommand("42 bis", "Descripción."), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ArticuloErrors.NumeroDuplicado);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private CrearArticuloHandler Handler() => new(_repositorio, _unidadDeTrabajo);
}
