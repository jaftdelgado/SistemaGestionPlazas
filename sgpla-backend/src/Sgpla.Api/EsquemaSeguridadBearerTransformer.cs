using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Sgpla.Api;

/// <summary>Declara el esquema Bearer en OpenAPI para que Scalar pueda enviar el token.</summary>
public sealed class EsquemaSeguridadBearerTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var componentes = document.Components ??= new OpenApiComponents();
        var esquemas = componentes.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        esquemas["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
        };

        return Task.CompletedTask;
    }
}
