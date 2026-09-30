using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Catalogos.Domain.Articulos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

/// <param name="Descripcion"><c>null</c> conserva la descripción actual.</param>
internal sealed record ModificarArticuloCommand(int Id, string Numero, string? Descripcion);

/// <summary>
/// Modifica número y descripción. La descripción siempre puede cambiar; el número solo mientras ningún Aviso use el
/// artículo. Las referencias están en otros módulos, así que se consulta a todas las
/// implementaciones de <see cref="IReferenciasArticulo"/>.
/// </summary>
internal sealed class ModificarArticuloHandler(
    IArticuloRepository repositorio,
    IEnumerable<IReferenciasArticulo> referencias,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarArticuloCommand>
{
    public async Task<Result> HandleAsync(ModificarArticuloCommand command, CancellationToken cancellationToken)
    {
        var articulo = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (articulo is null)
        {
            return ArticuloErrors.NoEncontrado(command.Id);
        }

        var (numeroAnterior, descripcionAnterior) = (articulo.Numero, articulo.Descripcion);
        var modificado = articulo.Modificar(command.Numero, command.Descripcion);
        if (modificado.IsFailure)
        {
            return modificado;
        }

        var cambiaNumero = !string.Equals(articulo.Numero, numeroAnterior, StringComparison.Ordinal);
        var cambiaDescripcion = !string.Equals(articulo.Descripcion, descripcionAnterior, StringComparison.Ordinal);
        if (!cambiaNumero && !cambiaDescripcion)
        {
            return Result.Success();
        }

        if (cambiaNumero)
        {
            foreach (var referencia in referencias)
            {
                if (await referencia.TieneReferenciasAsync(articulo.Id, cancellationToken))
                {
                    return ArticuloErrors.NumeroInmutable;
                }
            }

            if (await repositorio.ExisteNumeroAsync(articulo.Numero, articulo.Id, cancellationToken))
            {
                return ArticuloErrors.NumeroDuplicado;
            }
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
