using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

/// <summary>
/// NRC de una experiencia educativa en un periodo escolar. Solo lectura: la crea y la da de baja la
/// sincronización con PLANEA.
/// </summary>
internal sealed class ProgramacionAcademica : Entity, IEliminable
{
    public const int LongitudMaximaNrc = 20;

    private ProgramacionAcademica()
    {
    }

    public string Nrc { get; private set; } = string.Empty;

    public int PeriodoEscolarId { get; private set; }

    public int ExperienciaEducativaId { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }
}
