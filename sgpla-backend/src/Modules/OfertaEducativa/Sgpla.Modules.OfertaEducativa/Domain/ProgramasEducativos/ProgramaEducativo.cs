using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

/// <summary>
/// Programa que ofrece una entidad académica, con un sistema educativo y un nivel de formación. La
/// entidad no cambia; el sistema y el nivel solo cambian mientras el programa nunca haya tenido un plan de estudios.
/// Con baja lógica y sin restauración.
/// </summary>
internal sealed class ProgramaEducativo : Entity, IEliminable
{
    public const int LongitudMaximaNombre = 200;

    private ProgramaEducativo()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public int EntidadAcademicaId { get; private set; }

    public int SistemaEducativoId { get; private set; }

    public int NivelFormacionId { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    /// <summary>Valida el nombre; que la entidad, el sistema y el nivel existan lo comprueba el handler.</summary>
    public static Result<ProgramaEducativo> Crear(
        string nombre,
        int entidadAcademicaId,
        int sistemaEducativoId,
        int nivelFormacionId)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        return new ProgramaEducativo
        {
            Nombre = nombreNormalizado.Value,
            EntidadAcademicaId = entidadAcademicaId,
            SistemaEducativoId = sistemaEducativoId,
            NivelFormacionId = nivelFormacionId,
        };
    }

    /// <summary>
    /// Reemplaza el nombre, el sistema y el nivel. Con <paramref name="tuvoPlanes"/>, el sistema y el nivel no pueden
    /// cambiar. No asigna nada si algo falla.
    /// </summary>
    public Result Modificar(string nombre, int sistemaEducativoId, int nivelFormacionId, bool tuvoPlanes)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        if (tuvoPlanes && (sistemaEducativoId != SistemaEducativoId || nivelFormacionId != NivelFormacionId))
        {
            return ProgramaEducativoErrors.ClasificacionInmutable;
        }

        Nombre = nombreNormalizado.Value;
        SistemaEducativoId = sistemaEducativoId;
        NivelFormacionId = nivelFormacionId;
        return Result.Success();
    }

    public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;

    private static Result<string> NormalizarNombre(string? nombre)
    {
        var normalizado = Normalizacion.Texto(nombre);

        if (normalizado.Length == 0)
        {
            return ProgramaEducativoErrors.NombreVacio;
        }

        if (normalizado.Length > LongitudMaximaNombre)
        {
            return ProgramaEducativoErrors.NombreDemasiadoLargo;
        }

        return normalizado;
    }
}
