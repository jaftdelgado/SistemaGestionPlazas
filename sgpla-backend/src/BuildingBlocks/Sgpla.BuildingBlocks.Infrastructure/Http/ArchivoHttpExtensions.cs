using Microsoft.AspNetCore.Http;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Http;

public static class ArchivoHttpExtensions
{
    public static ArchivoRecibido? ComoArchivoRecibido(this IFormFile? archivo) =>
        archivo is null
            ? null
            : new ArchivoRecibido(archivo.FileName, archivo.ContentType, archivo.Length, archivo.OpenReadStream);
}
