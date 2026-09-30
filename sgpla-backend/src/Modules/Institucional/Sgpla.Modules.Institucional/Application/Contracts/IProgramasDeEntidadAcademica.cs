namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Lo implementa OfertaEducativa.</summary>
public interface IProgramasDeEntidadAcademica
{
    /// <summary>Con programas activos, la entidad no se da de baja.</summary>
    Task<bool> TieneProgramasActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken);

    /// <summary>Con cualquier programa, incluidos los dados de baja, el área de la entidad no cambia.</summary>
    Task<bool> TieneProgramasAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
