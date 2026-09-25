using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Domain.GradosAcademicos;

internal static class GradoAcademicoErrors
{
    public static Error NoEncontrado(int id) => Error.NotFound(
        "GradoAcademico.NoEncontrado", $"No existe el grado académico {id}.");
}
