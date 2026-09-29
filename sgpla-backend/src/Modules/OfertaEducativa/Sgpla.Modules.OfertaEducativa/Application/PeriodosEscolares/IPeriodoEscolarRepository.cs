using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

namespace Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;

internal interface IPeriodoEscolarRepository
{
    /// <summary>Solo activos.</summary>
    Task<PeriodoEscolar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Incluye los dados de baja: la clave no se reutiliza.</summary>
    Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken);

    /// <summary>Cualquier programación del periodo, incluidas las dadas de baja (D11).</summary>
    Task<bool> TieneProgramacionesAsync(int periodoEscolarId, CancellationToken cancellationToken);

    void Agregar(PeriodoEscolar periodoEscolar);
}
