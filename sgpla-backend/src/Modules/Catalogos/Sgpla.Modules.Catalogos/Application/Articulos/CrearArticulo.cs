using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Domain.Articulos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

internal sealed record CrearArticuloCommand(string Numero, string Descripcion);

/// <summary>La forma de la entrada la valida <see cref="Articulo.Crear"/>; aquí solo queda la unicidad del número.</summary>
internal sealed class CrearArticuloHandler(
    IArticuloRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearArticuloCommand, ArticuloResponse>
{
    public async Task<Result<ArticuloResponse>> HandleAsync(
        CrearArticuloCommand command,
        CancellationToken cancellationToken)
    {
        var creado = Articulo.Crear(command.Numero, command.Descripcion);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        if (await repositorio.ExisteNumeroAsync(creado.Value.Numero, excluirId: null, cancellationToken))
        {
            return ArticuloErrors.NumeroDuplicado;
        }

        repositorio.Agregar(creado.Value);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return ArticuloResponse.Desde(creado.Value);
    }
}
