using FluentValidation;
using Sgpla.BuildingBlocks.Application;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.BuildingBlocks;

public sealed class ValidacionDecoratorTests
{
    [Fact]
    public async Task HandleAsync_ConEntradaInvalida_DevuelveErroresPorCampoSinLlamarAlHandler()
    {
        var handler = new HandlerDePrueba();
        var decorador = new ValidacionCommandDecorator<ComandoDePrueba>(handler, [new ComandoDePruebaValidator()]);

        var resultado = await decorador.HandleAsync(new ComandoDePrueba(string.Empty), TestContext.Current.CancellationToken);

        handler.Llamadas.ShouldBe(0);
        var error = resultado.Error.ShouldBeOfType<ValidationError>();
        error.Code.ShouldBe(ValidationError.CodigoEntradaInvalida);
        error.Errores.Keys.ShouldBe(["Nombre"]);
    }

    [Fact]
    public async Task HandleAsync_ConEntradaValida_LlamaAlHandler()
    {
        var handler = new HandlerDePrueba();
        var decorador = new ValidacionCommandDecorator<ComandoDePrueba>(handler, [new ComandoDePruebaValidator()]);

        var resultado = await decorador.HandleAsync(new ComandoDePrueba("valor"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        handler.Llamadas.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_SinValidators_LlamaAlHandler()
    {
        var handler = new HandlerDePrueba();
        var decorador = new ValidacionCommandDecorator<ComandoDePrueba>(handler, []);

        await decorador.HandleAsync(new ComandoDePrueba(string.Empty), TestContext.Current.CancellationToken);

        handler.Llamadas.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 20, "Pagina")]
    [InlineData(1, 0, "TamanoPagina")]
    [InlineData(1, Paginacion.TamanoMaximo + 1, "TamanoPagina")]
    public void PaginacionValidator_FueraDeRango_ReportaElParametro(int pagina, int tamanoPagina, string campo)
    {
        var validator = new PaginacionValidator<Paginacion>(p => p);

        var resultado = validator.Validate(new Paginacion(pagina, tamanoPagina));

        resultado.Errors.Select(e => e.PropertyName).ShouldBe([campo]);
    }

    private sealed record ComandoDePrueba(string Nombre);

    private sealed class ComandoDePruebaValidator : AbstractValidator<ComandoDePrueba>
    {
        public ComandoDePruebaValidator()
        {
            RuleFor(c => c.Nombre).NotEmpty();
        }
    }

    private sealed class HandlerDePrueba : ICommandHandler<ComandoDePrueba>
    {
        public int Llamadas { get; private set; }

        public Task<Result> HandleAsync(ComandoDePrueba command, CancellationToken cancellationToken)
        {
            Llamadas++;
            return Task.FromResult(Result.Success());
        }
    }
}
