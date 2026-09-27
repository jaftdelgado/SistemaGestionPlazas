using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Sgpla.SharedKernel;

namespace Sgpla.BuildingBlocks.Infrastructure.Http;

/// <summary>Traducción de un <see cref="Result"/> a la respuesta HTTP más común; el error siempre sale con <see cref="ErrorHttpExtensions.ToProblem"/>.</summary>
public static class ResultHttpExtensions
{
    /// <summary>200 con el valor, o el ProblemDetails del error.</summary>
    public static Results<Ok<T>, ProblemHttpResult> ToOk<T>(this Result<T> resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return resultado.IsSuccess ? TypedResults.Ok(resultado.Value) : resultado.Error.ToProblem();
    }

    /// <summary>204, o el ProblemDetails del error.</summary>
    public static Results<NoContent, ProblemHttpResult> ToNoContent(this Result resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return resultado.IsSuccess ? TypedResults.NoContent() : resultado.Error.ToProblem();
    }
}
