using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

internal static class PlanEstudiosErrors
{
    public static readonly Error CodigoVacio = Error.Validation(
        "PlanEstudios.CodigoVacio", "El código es obligatorio.", nameof(PlanEstudios.Codigo));

    public static readonly Error CodigoDemasiadoLargo = Error.Validation(
        "PlanEstudios.CodigoDemasiadoLargo",
        $"El código admite hasta {PlanEstudios.LongitudMaximaCodigo} caracteres.",
        nameof(PlanEstudios.Codigo));

    public static readonly Error CodigoFormatoInvalido = Error.Validation(
        "PlanEstudios.CodigoFormatoInvalido",
        "El código solo admite letras de la A a la Z sin acentos, dígitos y guiones.",
        nameof(PlanEstudios.Codigo));

    public static readonly Error SinExperiencias = Error.Validation(
        "PlanEstudios.SinExperiencias",
        "El plan debe incluir al menos una experiencia educativa.",
        nameof(PlanEstudios.ExperienciasEducativas));

    public static readonly Error DemasiadasExperiencias = Error.Validation(
        "PlanEstudios.DemasiadasExperiencias",
        $"El plan admite hasta {PlanEstudios.MaximoExperienciasImportacion} experiencias educativas por importación.",
        nameof(PlanEstudios.ExperienciasEducativas));

    public static readonly Error ProgramaEducativoInexistente = Error.Validation(
        "PlanEstudios.ProgramaEducativoInexistente",
        "No existe un programa educativo activo con ese id en tu ámbito.",
        nameof(PlanEstudios.ProgramaEducativoId));

    public static readonly Error CodigoDuplicado = Error.Conflict(
        "PlanEstudios.CodigoDuplicado", "El programa educativo ya tiene un plan con ese código.");

    public static readonly Error ExperienciasConProgramacionesActivas = Error.Conflict(
        "PlanEstudios.ExperienciasConProgramacionesActivas",
        "Alguna experiencia educativa del plan tiene programaciones activas.");

    /// <param name="indice">Posición (desde 0) de la segunda EE con la misma materia y curso.</param>
    public static Error ExperienciaRepetida(int indice) => Error.Validation(
        "PlanEstudios.ExperienciaRepetida",
        "La materia y el curso ya aparecen en otra experiencia educativa del plan.",
        CampoDeExperiencia(indice, nameof(ExperienciaEducativa.Curso)));

    /// <param name="indice">Posición (desde 0) del elemento nulo.</param>
    public static Error ExperienciaVacia(int indice) => Error.Validation(
        "PlanEstudios.ExperienciaVacia",
        "La experiencia educativa no puede estar vacía.",
        $"{nameof(PlanEstudios.ExperienciasEducativas)}[{indice}]");

    /// <param name="indice">Posición (desde 0) de la primera EE con un área inexistente.</param>
    public static Error AreaFormacionInexistente(int indice) => Error.Validation(
        "PlanEstudios.AreaFormacionInexistente",
        "No existe el área de formación indicada.",
        CampoDeExperiencia(indice, nameof(ExperienciaEducativa.AreaFormacionId)));

    public static Error NoEncontrado(int id) => Error.NotFound(
        "PlanEstudios.NoEncontrado", $"No existe el plan de estudios {id}.");

    private static string CampoDeExperiencia(int indice, string campo) =>
        $"{nameof(PlanEstudios.ExperienciasEducativas)}[{indice}].{campo}";
}
