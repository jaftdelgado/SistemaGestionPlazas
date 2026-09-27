using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.Ubicaciones;

internal static class UbicacionErrors
{
    public static Error RegionNoEncontrada(int id) => Error.NotFound(
        "Region.NoEncontrado", $"No existe la región {id}.");

    public static Error CampusNoEncontrado(int id) => Error.NotFound(
        "Campus.NoEncontrado", $"No existe el campus {id}.");
}
