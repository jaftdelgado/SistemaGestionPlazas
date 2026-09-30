namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Lo implementa Usuarios. Con usuarios de entidad activos, la entidad no se da de baja.</summary>
public interface IUsuariosDeEntidadAcademica
{
    Task<bool> TieneUsuariosActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
