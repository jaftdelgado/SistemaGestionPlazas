using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

/// <summary>
/// Sesión semanal de una programación. Solo guarda el snapshot vigente de PLANEA, sin baja lógica.
/// Solo lectura: la crea y la reemplaza la sincronización.
/// </summary>
internal sealed class HorarioProgramacion : Entity
{
    public const int LongitudMaximaEspacio = 100;

    private HorarioProgramacion()
    {
    }

    public int ProgramacionAcademicaId { get; private set; }

    /// <summary>Sin navegación: la tabla de sincronizaciones es de Integracion.</summary>
    public int SincronizacionPlaneaId { get; private set; }

    /// <summary>1 = lunes … 6 = sábado.</summary>
    public byte DiaSemana { get; private set; }

    public TimeOnly HoraInicio { get; private set; }

    public TimeOnly HoraFin { get; private set; }

    public DateOnly FechaInicio { get; private set; }

    public DateOnly FechaFin { get; private set; }

    public string? Edificio { get; private set; }

    public string? Aula { get; private set; }
}
