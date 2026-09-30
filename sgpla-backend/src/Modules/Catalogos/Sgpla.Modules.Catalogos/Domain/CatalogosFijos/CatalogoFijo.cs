using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>
/// Base de los catálogos fijos: sus valores los carga la semilla con ids estables y ningún usuario
/// los agrega, modifica ni elimina, así que no tienen fábrica ni comportamiento. Cada catálogo es una subclase
/// sellada que declara la longitud de su nombre y, si las tiene, sus columnas adicionales.
/// </summary>
internal abstract class CatalogoFijo : Entity
{
    public string Nombre { get; protected set; } = string.Empty;
}
