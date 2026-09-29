using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

/// <summary>
/// Periodo al que pertenecen las programaciones (DATABASE.md §6.12). La clave es inmutable; las fechas cambian en
/// cualquier momento (Modulo_OfertaEducativa.md, decisión D11). Con baja lógica y sin restauración.
/// </summary>
internal sealed class PeriodoEscolar : Entity, IEliminable
{
    public const int LongitudClave = 6;

    private PeriodoEscolar()
    {
    }

    public string Clave { get; private set; } = string.Empty;

    public DateOnly FechaInicio { get; private set; }

    public DateOnly FechaFin { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    public static Result<PeriodoEscolar> Crear(string clave, DateOnly fechaInicio, DateOnly fechaFin)
    {
        var claveNormalizada = NormalizarClave(clave);
        if (claveNormalizada.IsFailure)
        {
            return claveNormalizada.Error;
        }

        var rango = ValidarRango(fechaInicio, fechaFin);
        if (rango.IsFailure)
        {
            return rango.Error;
        }

        return new PeriodoEscolar { Clave = claveNormalizada.Value, FechaInicio = fechaInicio, FechaFin = fechaFin };
    }

    /// <summary>Reemplaza las fechas; la clave no cambia. Si el rango no es válido, no asigna nada.</summary>
    public Result Modificar(DateOnly fechaInicio, DateOnly fechaFin)
    {
        var rango = ValidarRango(fechaInicio, fechaFin);
        if (rango.IsFailure)
        {
            return rango;
        }

        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        return Result.Success();
    }

    public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;

    private static Result<string> NormalizarClave(string? clave)
    {
        var normalizada = Normalizacion.Recortar(clave);

        if (normalizada.Length == 0)
        {
            return PeriodoEscolarErrors.ClaveVacia;
        }

        if (normalizada.Length != LongitudClave || !Normalizacion.SonDigitos(normalizada))
        {
            return PeriodoEscolarErrors.ClaveFormatoInvalido;
        }

        return normalizada;
    }

    private static Result ValidarRango(DateOnly fechaInicio, DateOnly fechaFin) =>
        fechaInicio <= fechaFin ? Result.Success() : PeriodoEscolarErrors.RangoFechasInvalido;
}
