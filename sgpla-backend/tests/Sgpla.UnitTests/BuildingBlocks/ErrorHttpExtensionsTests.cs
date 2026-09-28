using Microsoft.AspNetCore.Http;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.BuildingBlocks;

public sealed class ErrorHttpExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    public void ToProblem_TraduceCadaTipoDeErrorAlCodigoHttpQueLeCorresponde(ErrorType tipo, int codigoEsperado)
    {
        var error = new Error("Prueba.Codigo", "Mensaje de prueba.", tipo);

        var resultado = error.ToProblem();

        resultado.ProblemDetails.Status.ShouldBe(codigoEsperado);
        resultado.ProblemDetails.Extensions[ErrorHttpExtensions.ExtensionCodigo].ShouldBe(error.Code);
    }
}
