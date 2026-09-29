namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Lo implementa OfertaEducativa (P1 y P2 de pendientes.md).</summary>
public interface IProgramasDeEntidadAcademica
{
    /// <summary>Con programas activos, la entidad no se da de baja (Modulo_OfertaEducativa.md, D10).</summary>
    Task<bool> TieneProgramasActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken);

    /// <summary>Con cualquier programa, incluidos los dados de baja, el área de la entidad no cambia (DATABASE.md §10).</summary>
    Task<bool> TieneProgramasAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
