using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.AreasAcademicas;

internal sealed record ModificarAreaAcademicaCommand(int Id, string Nombre, string Telefono, string? Extension);

internal sealed class ModificarAreaAcademicaHandler(
    IAreaAcademicaRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarAreaAcademicaCommand>
{
    public async Task<Result> HandleAsync(ModificarAreaAcademicaCommand command, CancellationToken cancellationToken)
    {
        var area = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (area is null)
        {
            return AreaAcademicaErrors.NoEncontrado(command.Id);
        }

        var modificada = area.Modificar(command.Nombre, command.Telefono, command.Extension);
        if (modificada.IsFailure)
        {
            return modificada;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
