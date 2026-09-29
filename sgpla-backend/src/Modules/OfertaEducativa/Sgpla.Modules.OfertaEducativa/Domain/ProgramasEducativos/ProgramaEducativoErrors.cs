using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

internal static class ProgramaEducativoErrors
{
    public static readonly Error NombreVacio = Error.Validation(
        "ProgramaEducativo.NombreVacio", "El nombre es obligatorio.", nameof(ProgramaEducativo.Nombre));

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "ProgramaEducativo.NombreDemasiadoLargo",
        $"El nombre admite hasta {ProgramaEducativo.LongitudMaximaNombre} caracteres.",
        nameof(ProgramaEducativo.Nombre));

    public static readonly Error EntidadAcademicaInexistente = Error.Validation(
        "ProgramaEducativo.EntidadAcademicaInexistente",
        "No existe una entidad académica activa con ese id en tu ámbito.",
        nameof(ProgramaEducativo.EntidadAcademicaId));

    public static readonly Error SistemaEducativoInexistente = Error.Validation(
        "ProgramaEducativo.SistemaEducativoInexistente",
        "No existe el sistema educativo indicado.",
        nameof(ProgramaEducativo.SistemaEducativoId));

    public static readonly Error NivelFormacionInexistente = Error.Validation(
        "ProgramaEducativo.NivelFormacionInexistente",
        "No existe el nivel de formación indicado.",
        nameof(ProgramaEducativo.NivelFormacionId));

    public static readonly Error NombreDuplicado = Error.Conflict(
        "ProgramaEducativo.NombreDuplicado",
        "La entidad académica ya tiene un programa con ese nombre en ese sistema educativo.");

    public static readonly Error ClasificacionInmutable = Error.Conflict(
        "ProgramaEducativo.ClasificacionInmutable",
        "El sistema educativo y el nivel de formación no cambian una vez que el programa tuvo un plan de estudios.");

    public static readonly Error TienePlanesActivos = Error.Conflict(
        "ProgramaEducativo.TienePlanesActivos", "El programa educativo tiene planes de estudio activos.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "ProgramaEducativo.NoEncontrado", $"No existe el programa educativo {id}.");
}
