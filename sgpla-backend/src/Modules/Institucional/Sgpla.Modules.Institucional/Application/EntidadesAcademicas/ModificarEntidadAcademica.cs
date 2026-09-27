using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal sealed record ModificarEntidadAcademicaCommand(
    int Id,
    string Nombre,
    string Calle,
    string? NumeroExterior,
    string Colonia,
    string CodigoPostal,
    string Telefono,
    string? Extension,
    int AreaAcademicaId,
    int MunicipioId);

/// <summary>El área siempre puede cambiar (Modulo_Institucional.md, decisión D6; ver pendientes.md).</summary>
internal sealed class ModificarEntidadAcademicaHandler(
    IEntidadAcademicaRepository repositorio,
    IMunicipios municipios,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarEntidadAcademicaCommand>
{
    public async Task<Result> HandleAsync(ModificarEntidadAcademicaCommand command, CancellationToken cancellationToken)
    {
        var entidad = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (entidad is null)
        {
            return EntidadAcademicaErrors.NoEncontrado(command.Id);
        }

        var modificada = entidad.Modificar(
            command.Nombre,
            command.Calle,
            command.NumeroExterior,
            command.Colonia,
            command.CodigoPostal,
            command.Telefono,
            command.Extension,
            command.AreaAcademicaId,
            command.MunicipioId);
        if (modificada.IsFailure)
        {
            return modificada;
        }

        if (!await repositorio.ExisteAreaAcademicaActivaAsync(entidad.AreaAcademicaId, cancellationToken))
        {
            return EntidadAcademicaErrors.AreaAcademicaInexistente;
        }

        if (!await municipios.ExisteAsync(entidad.MunicipioId, cancellationToken))
        {
            return EntidadAcademicaErrors.MunicipioInexistente;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
