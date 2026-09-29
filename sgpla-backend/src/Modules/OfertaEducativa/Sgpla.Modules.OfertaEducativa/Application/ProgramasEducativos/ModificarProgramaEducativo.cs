using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

internal sealed record ModificarProgramaEducativoCommand(int Id, string Nombre, int SistemaEducativoId, int NivelFormacionId);

/// <summary>
/// Un programa fuera del ámbito del DGAA responde <c>NoEncontrado</c> (D15). La unicidad se vuelve a comprobar siempre,
/// porque puede cambiar el nombre o el sistema. Si algo falla después de <see cref="ProgramaEducativo.Modificar"/>, no
/// se guarda.
/// </summary>
internal sealed class ModificarProgramaEducativoHandler(
    IProgramaEducativoRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarProgramaEducativoCommand>
{
    public async Task<Result> HandleAsync(ModificarProgramaEducativoCommand command, CancellationToken cancellationToken)
    {
        var programa = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (programa is null || !await ambito.PuedeEscribirEnEntidadAsync(programa.EntidadAcademicaId, cancellationToken))
        {
            return ProgramaEducativoErrors.NoEncontrado(command.Id);
        }

        var tuvoPlanes = await repositorio.TuvoPlanesAsync(programa.Id, cancellationToken);

        var modificado = programa.Modificar(command.Nombre, command.SistemaEducativoId, command.NivelFormacionId, tuvoPlanes);
        if (modificado.IsFailure)
        {
            return modificado;
        }

        var sistemas = await clasificaciones.ObtenerSistemasEducativosAsync([programa.SistemaEducativoId], cancellationToken);
        if (!sistemas.ContainsKey(programa.SistemaEducativoId))
        {
            return ProgramaEducativoErrors.SistemaEducativoInexistente;
        }

        var niveles = await clasificaciones.ObtenerNivelesFormacionAsync([programa.NivelFormacionId], cancellationToken);
        if (!niveles.ContainsKey(programa.NivelFormacionId))
        {
            return ProgramaEducativoErrors.NivelFormacionInexistente;
        }

        if (await repositorio.ExisteNombreAsync(
                programa.EntidadAcademicaId, programa.Nombre, programa.SistemaEducativoId, programa.Id, cancellationToken))
        {
            return ProgramaEducativoErrors.NombreDuplicado;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
