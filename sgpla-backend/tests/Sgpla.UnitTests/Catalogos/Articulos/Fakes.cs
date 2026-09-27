using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.UnitTests.Catalogos.Articulos;

/// <summary>Repositorio en memoria. La unicidad se simula con comparación ordinal del número normalizado.</summary>
internal sealed class ArticuloRepositoryFalso : IArticuloRepository
{
    private readonly Dictionary<int, Articulo> _porId = [];

    public List<Articulo> Agregados { get; } = [];

    public HashSet<string> NumerosExistentes { get; } = new(StringComparer.Ordinal);

    public void Registrar(int id, Articulo articulo) => _porId[id] = articulo;

    public Task<Articulo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId, CancellationToken cancellationToken) =>
        Task.FromResult(NumerosExistentes.Contains(numero));

    public void Agregar(Articulo articulo) => Agregados.Add(articulo);
}

internal sealed class ReferenciasArticuloFalsas(bool tieneReferencias) : IReferenciasArticulo
{
    public int Consultas { get; private set; }

    public Task<bool> TieneReferenciasAsync(int articuloId, CancellationToken cancellationToken)
    {
        Consultas++;
        return Task.FromResult(tieneReferencias);
    }
}

internal sealed class UnitOfWorkFalso : IUnitOfWork
{
    public int Guardados { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Guardados++;
        return Task.CompletedTask;
    }
}
