namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Lo implementa Usuarios. Con usuarios DGAA activos, el área no se da de baja.</summary>
public interface IUsuariosDeAreaAcademica
{
    Task<bool> TieneUsuariosActivosAsync(int areaAcademicaId, CancellationToken cancellationToken);
}
