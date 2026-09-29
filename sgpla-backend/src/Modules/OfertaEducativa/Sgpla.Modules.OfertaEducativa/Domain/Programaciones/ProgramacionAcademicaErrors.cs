using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

internal static class ProgramacionAcademicaErrors
{
    public static Error NoEncontrado(int id) => Error.NotFound(
        "ProgramacionAcademica.NoEncontrado", $"No existe la programación académica {id}.");
}
