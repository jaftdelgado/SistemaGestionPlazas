using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal sealed record CrearEntidadAcademicaCommand(
    string Clave,
    string Nombre,
    string Calle,
    string? NumeroExterior,
    string Colonia,
    string CodigoPostal,
    string Telefono,
    string? Extension,
    int CampusId,
    int AreaAcademicaId,
    int MunicipioId);

internal sealed class CrearEntidadAcademicaHandler(
    IEntidadAcademicaRepository repositorio,
    IMunicipios municipios,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearEntidadAcademicaCommand, int>
{
    public async Task<Result<int>> HandleAsync(CrearEntidadAcademicaCommand command, CancellationToken cancellationToken)
    {
        var creada = EntidadAcademica.Crear(
            command.Clave,
            command.Nombre,
            command.Calle,
            command.NumeroExterior,
            command.Colonia,
            command.CodigoPostal,
            command.Telefono,
            command.Extension,
            command.CampusId,
            command.AreaAcademicaId,
            command.MunicipioId);
        if (creada.IsFailure)
        {
            return creada.Error;
        }

        var entidad = creada.Value;

        if (!await repositorio.ExisteCampusAsync(entidad.CampusId, cancellationToken))
        {
            return EntidadAcademicaErrors.CampusInexistente;
        }

        if (!await repositorio.ExisteAreaAcademicaActivaAsync(entidad.AreaAcademicaId, cancellationToken))
        {
            return EntidadAcademicaErrors.AreaAcademicaInexistente;
        }

        if (!await municipios.ExisteAsync(entidad.MunicipioId, cancellationToken))
        {
            return EntidadAcademicaErrors.MunicipioInexistente;
        }

        if (await repositorio.ExisteClaveAsync(entidad.Clave, cancellationToken))
        {
            return EntidadAcademicaErrors.ClaveDuplicada;
        }

        repositorio.Agregar(entidad);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return entidad.Id;
    }
}
