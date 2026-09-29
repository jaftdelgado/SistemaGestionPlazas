using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

internal sealed record CrearProgramaEducativoCommand(
    string Nombre,
    int EntidadAcademicaId,
    int SistemaEducativoId,
    int NivelFormacionId);

/// <summary>
/// Solo el DGAA del área de la entidad crea programas; una entidad fuera de su ámbito se trata como inexistente
/// (Modulo_OfertaEducativa.md, D9 y D15).
/// </summary>
internal sealed class CrearProgramaEducativoHandler(
    IProgramaEducativoRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearProgramaEducativoCommand, int>
{
    public async Task<Result<int>> HandleAsync(CrearProgramaEducativoCommand command, CancellationToken cancellationToken)
    {
        var creado = ProgramaEducativo.Crear(
            command.Nombre, command.EntidadAcademicaId, command.SistemaEducativoId, command.NivelFormacionId);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        var programa = creado.Value;

        if (!await ambito.PuedeEscribirEnEntidadAsync(programa.EntidadAcademicaId, cancellationToken))
        {
            return ProgramaEducativoErrors.EntidadAcademicaInexistente;
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
                programa.EntidadAcademicaId, programa.Nombre, programa.SistemaEducativoId, excluirId: null, cancellationToken))
        {
            return ProgramaEducativoErrors.NombreDuplicado;
        }

        repositorio.Agregar(programa);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return programa.Id;
    }
}
