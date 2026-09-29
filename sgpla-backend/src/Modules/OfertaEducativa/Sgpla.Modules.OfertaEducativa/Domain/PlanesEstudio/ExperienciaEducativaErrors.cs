using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

internal static class ExperienciaEducativaErrors
{
    public static readonly Error NombreVacio = Error.Validation(
        "ExperienciaEducativa.NombreVacio", "El nombre es obligatorio.", nameof(ExperienciaEducativa.Nombre));

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "ExperienciaEducativa.NombreDemasiadoLargo",
        $"El nombre admite hasta {ExperienciaEducativa.LongitudMaximaNombre} caracteres.",
        nameof(ExperienciaEducativa.Nombre));

    public static readonly Error MateriaVacia = Error.Validation(
        "ExperienciaEducativa.MateriaVacia", "La materia es obligatoria.", nameof(ExperienciaEducativa.Materia));

    public static readonly Error MateriaDemasiadoLarga = Error.Validation(
        "ExperienciaEducativa.MateriaDemasiadoLarga",
        $"La materia admite hasta {ExperienciaEducativa.LongitudMaximaMateria} caracteres.",
        nameof(ExperienciaEducativa.Materia));

    public static readonly Error MateriaFormatoInvalido = Error.Validation(
        "ExperienciaEducativa.MateriaFormatoInvalido",
        "La materia solo admite letras de la A a la Z sin acentos y dígitos.",
        nameof(ExperienciaEducativa.Materia));

    public static readonly Error CursoVacio = Error.Validation(
        "ExperienciaEducativa.CursoVacio", "El curso es obligatorio.", nameof(ExperienciaEducativa.Curso));

    public static readonly Error CursoDemasiadoLargo = Error.Validation(
        "ExperienciaEducativa.CursoDemasiadoLargo",
        $"El curso admite hasta {ExperienciaEducativa.LongitudMaximaCurso} caracteres.",
        nameof(ExperienciaEducativa.Curso));

    public static readonly Error CursoFormatoInvalido = Error.Validation(
        "ExperienciaEducativa.CursoFormatoInvalido",
        "El curso solo admite letras de la A a la Z sin acentos y dígitos.",
        nameof(ExperienciaEducativa.Curso));

    public static readonly Error HorasTeoricasNegativas = Error.Validation(
        "ExperienciaEducativa.HorasTeoricasNegativas",
        "Las horas teóricas no pueden ser negativas.",
        nameof(ExperienciaEducativa.HorasTeoricas));

    public static readonly Error HorasPracticasNegativas = Error.Validation(
        "ExperienciaEducativa.HorasPracticasNegativas",
        "Las horas prácticas no pueden ser negativas.",
        nameof(ExperienciaEducativa.HorasPracticas));

    public static readonly Error CreditosNoPositivos = Error.Validation(
        "ExperienciaEducativa.CreditosNoPositivos",
        "Los créditos deben ser mayores que cero.",
        nameof(ExperienciaEducativa.Creditos));

    public static readonly Error CupoMinimoNegativo = Error.Validation(
        "ExperienciaEducativa.CupoMinimoNegativo",
        "El cupo mínimo no puede ser negativo.",
        nameof(ExperienciaEducativa.CupoMinimo));

    public static readonly Error CupoMaximoNegativo = Error.Validation(
        "ExperienciaEducativa.CupoMaximoNegativo",
        "El cupo máximo no puede ser negativo.",
        nameof(ExperienciaEducativa.CupoMaximo));

    public static readonly Error CuposInvertidos = Error.Validation(
        "ExperienciaEducativa.CuposInvertidos",
        "El cupo mínimo no puede ser mayor que el cupo máximo.",
        nameof(ExperienciaEducativa.CupoMinimo));

    public static readonly Error PlanEstudiosInexistente = Error.Validation(
        "ExperienciaEducativa.PlanEstudiosInexistente",
        "No existe un plan de estudios activo con ese id en tu ámbito.",
        nameof(ExperienciaEducativa.PlanEstudiosId));

    public static readonly Error AreaFormacionInexistente = Error.Validation(
        "ExperienciaEducativa.AreaFormacionInexistente",
        "No existe el área de formación indicada.",
        nameof(ExperienciaEducativa.AreaFormacionId));

    public static readonly Error MateriaCursoDuplicado = Error.Conflict(
        "ExperienciaEducativa.MateriaCursoDuplicado",
        "El plan ya tiene una experiencia educativa con esa materia y ese curso.");

    public static readonly Error AtributosCurricularesInmutables = Error.Conflict(
        "ExperienciaEducativa.AtributosCurricularesInmutables",
        "Las horas, los créditos y el área de formación no cambian una vez que la experiencia educativa fue programada.");

    public static readonly Error TieneProgramacionesActivas = Error.Conflict(
        "ExperienciaEducativa.TieneProgramacionesActivas", "La experiencia educativa tiene programaciones activas.");

    public static readonly Error TieneReferencias = Error.Conflict(
        "ExperienciaEducativa.TieneReferencias",
        "La experiencia educativa tiene solicitudes u otros registros vigentes que la usan.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "ExperienciaEducativa.NoEncontrado", $"No existe la experiencia educativa {id}.");
}
