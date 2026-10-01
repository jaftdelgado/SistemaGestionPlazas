using Scalar.AspNetCore;
using Sgpla.Api;
using Sgpla.BuildingBlocks.Infrastructure.Archivos;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Aspirantes;
using Sgpla.Modules.Catalogos;
using Sgpla.Modules.ConsejoTecnico;
using Sgpla.Modules.Docentes;
using Sgpla.Modules.Institucional;
using Sgpla.Modules.Integracion;
using Sgpla.Modules.OfertaEducativa;
using Sgpla.Modules.Publicacion;
using Sgpla.Modules.SolicitudesApertura;
using Sgpla.Modules.Usuarios;

const string PoliticaCors = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Correlación: los scopes de log llevan el TraceId que devuelve ProblemDetails.
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ViolacionUnicidadExceptionHandler>();
builder.Services.AddExceptionHandler<ConcurrenciaExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<EsquemaSeguridadBearerTransformer>());
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCors(options => options.AddPolicy(PoliticaCors, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddPersistenciaSgpla(builder.Configuration);
builder.Services.AddAlmacenamientoArchivos(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SgplaDbContext>("sqlserver");

builder.Services
    .AddInstitucionalModule(builder.Configuration)
    .AddCatalogosModule(builder.Configuration)
    .AddUsuariosModule(builder.Configuration)
    .AddOfertaEducativaModule(builder.Configuration)
    .AddDocentesModule(builder.Configuration)
    .AddIntegracionModule(builder.Configuration)
    .AddSolicitudesAperturaModule(builder.Configuration)
    .AddPublicacionModule(builder.Configuration)
    .AddAspirantesModule(builder.Configuration)
    .AddConsejoTecnicoModule(builder.Configuration);

var app = builder.Build();

if (args is ["bootstrap-superusuario"])
{
    return await UsuariosModule.EjecutarBootstrapAsync(app.Services, Console.Out, CancellationToken.None);
}

// Un request mal formado (JSON inválido, parámetro no convertible) responde su 400 también en Development,
// donde el enlace de parámetros lanza la excepción en lugar de responder directamente.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = excepcion => excepcion is BadHttpRequestException solicitudInvalida
        ? solicitudInvalida.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app
    .MapInstitucionalEndpoints()
    .MapCatalogosEndpoints()
    .MapUsuariosEndpoints()
    .MapOfertaEducativaEndpoints()
    .MapDocentesEndpoints()
    .MapIntegracionEndpoints()
    .MapSolicitudesAperturaEndpoints()
    .MapPublicacionEndpoints()
    .MapAspirantesEndpoints()
    .MapConsejoTecnicoEndpoints();

await app.RunAsync();
return 0;

/// <summary>Punto de entrada expuesto para <c>WebApplicationFactory</c> en las pruebas de integración.</summary>
public partial class Program;
