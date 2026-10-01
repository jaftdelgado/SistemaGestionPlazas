using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.SolicitudesApertura;

internal static class ConversionesUtc
{
    /// <summary>Las columnas datetime2 guardan UTC sin zona; al leerlas se marcan como UTC.</summary>
    public static readonly ValueConverter<DateTime, DateTime> Utc =
        new(valor => valor, valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
}
