using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SharedKernel;

public sealed class ResultTests
{
    private static readonly Error ErrorDePrueba = Error.Conflict("Prueba.Conflicto", "Conflicto de prueba.");

    [Fact]
    public void Success_NoTieneError()
    {
        var resultado = Result.Success();

        resultado.IsSuccess.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => resultado.Error);
    }

    [Fact]
    public void ConversionDesdeError_EsFallida()
    {
        Result resultado = ErrorDePrueba;

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.ShouldBe(ErrorDePrueba);
    }

    [Fact]
    public void ConversionDesdeValor_EsExitosaConElValor()
    {
        Result<int> resultado = 42;

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe(42);
    }

    [Fact]
    public void SuccessConValorDeInterfaz_EsExitosaConElValor()
    {
        IReadOnlyList<int> valores = [1, 2];

        var resultado = Result.Success(valores);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBeSameAs(valores);
    }

    [Fact]
    public void ResultadoFallido_NoTieneValor()
    {
        Result<int> resultado = ErrorDePrueba;

        resultado.Error.ShouldBe(ErrorDePrueba);
        Should.Throw<InvalidOperationException>(() => resultado.Value);
    }

    [Fact]
    public void Error_DeclaraSuTipo()
    {
        Error.Validation("A", "a").Type.ShouldBe(ErrorType.Validation);
        Error.NotFound("B", "b").Type.ShouldBe(ErrorType.NotFound);
        Error.Conflict("C", "c").Type.ShouldBe(ErrorType.Conflict);
    }
}
