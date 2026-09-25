namespace Sgpla.SharedKernel;

/// <summary>Clase base de las entidades persistentes: todas se identifican por un <c>int IDENTITY</c>.</summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}
