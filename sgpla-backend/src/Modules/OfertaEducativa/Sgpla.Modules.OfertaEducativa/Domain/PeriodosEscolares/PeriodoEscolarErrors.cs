using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

internal static class PeriodoEscolarErrors
{
    public static readonly Error ClaveVacia = Error.Validation(
        "PeriodoEscolar.ClaveVacia", "La clave es obligatoria.", nameof(PeriodoEscolar.Clave));

    public static readonly Error ClaveFormatoInvalido = Error.Validation(
        "PeriodoEscolar.ClaveFormatoInvalido",
        "La clave debe tener exactamente seis dígitos.",
        nameof(PeriodoEscolar.Clave));

    public static readonly Error RangoFechasInvalido = Error.Validation(
        "PeriodoEscolar.RangoFechasInvalido",
        "La fecha de fin no puede ser anterior a la fecha de inicio.",
        nameof(PeriodoEscolar.FechaFin));

    public static readonly Error ClaveDuplicada = Error.Conflict(
        "PeriodoEscolar.ClaveDuplicada", "Ya existe un periodo escolar con esa clave.");

    public static readonly Error TieneReferencias = Error.Conflict(
        "PeriodoEscolar.TieneReferencias",
        "El periodo escolar tiene programaciones u otros registros que lo usan.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "PeriodoEscolar.NoEncontrado", $"No existe el periodo escolar {id}.");
}
