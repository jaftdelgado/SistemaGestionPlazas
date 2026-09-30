using System.Reflection;
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

namespace Sgpla.ArchitectureTests;

/// <summary>
/// Módulos del sistema y dependencias permitidas entre ellos.
/// Al agregar un módulo o una dependencia, actualizar este mapa de forma deliberada.
/// </summary>
internal static class Modulos
{
    public static readonly IReadOnlyDictionary<string, Assembly> Ensamblados = new Dictionary<string, Assembly>
    {
        ["Institucional"] = typeof(InstitucionalModule).Assembly,
        ["Catalogos"] = typeof(CatalogosModule).Assembly,
        ["Usuarios"] = typeof(UsuariosModule).Assembly,
        ["OfertaEducativa"] = typeof(OfertaEducativaModule).Assembly,
        ["Docentes"] = typeof(DocentesModule).Assembly,
        ["Integracion"] = typeof(IntegracionModule).Assembly,
        ["SolicitudesApertura"] = typeof(SolicitudesAperturaModule).Assembly,
        ["Publicacion"] = typeof(PublicacionModule).Assembly,
        ["Aspirantes"] = typeof(AspirantesModule).Assembly,
        ["ConsejoTecnico"] = typeof(ConsejoTecnicoModule).Assembly,
    };

    public static readonly IReadOnlyDictionary<string, string[]> DependenciasPermitidas = new Dictionary<string, string[]>
    {
        ["Institucional"] = ["Catalogos"],
        ["Catalogos"] = [],
        ["Usuarios"] = ["Institucional"],
        ["OfertaEducativa"] = ["Institucional", "Catalogos"],
        ["Docentes"] = ["OfertaEducativa", "Catalogos"],
        ["Integracion"] = ["OfertaEducativa", "Docentes"],
        ["SolicitudesApertura"] = ["OfertaEducativa"],
        ["Publicacion"] = ["Institucional", "OfertaEducativa", "Catalogos"],
        ["Aspirantes"] = ["Publicacion", "Catalogos"],
        ["ConsejoTecnico"] = ["Publicacion", "Aspirantes", "Docentes", "Catalogos", "Institucional"],
    };

    public static string Namespace(string modulo) => $"Sgpla.Modules.{modulo}";

    /// <summary>Único namespace de un módulo que otros módulos pueden usar.</summary>
    public static string NamespaceContratos(string modulo) => $"{Namespace(modulo)}.Application.Contracts";

    public static string NombreClaseModulo(string modulo) => $"{modulo}Module";

    public static TheoryData<string> Nombres()
    {
        var datos = new TheoryData<string>();
        foreach (var nombre in Ensamblados.Keys)
        {
            datos.Add(nombre);
        }

        return datos;
    }
}
