namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Lo implementa Usuarios. Con usuarios de entidad activos, la entidad no se da de baja (DATABASE.md §9.2).</summary>
public interface IUsuariosDeEntidadAcademica
{
    Task<bool> TieneUsuariosActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
