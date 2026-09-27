using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.AreasAcademicas;

internal sealed record CrearAreaAcademicaCommand(int Clave, string Nombre, string Telefono, string? Extension);

internal sealed class CrearAreaAcademicaHandler(
    IAreaAcademicaRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearAreaAcademicaCommand, AreaAcademicaResponse>
{
    public async Task<Result<AreaAcademicaResponse>> HandleAsync(
        CrearAreaAcademicaCommand command,
        CancellationToken cancellationToken)
    {
        var creada = AreaAcademica.Crear(command.Clave, command.Nombre, command.Telefono, command.Extension);
        if (creada.IsFailure)
        {
            return creada.Error;
        }

        if (await repositorio.ExisteClaveAsync(creada.Value.Clave, cancellationToken))
        {
            return AreaAcademicaErrors.ClaveDuplicada;
        }

        repositorio.Agregar(creada.Value);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return AreaAcademicaResponse.Desde(creada.Value);
    }
}
