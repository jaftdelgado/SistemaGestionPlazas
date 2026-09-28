using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.Usuarios;

internal sealed class UsuarioRepositoryFalso : IUsuarioRepository
{
    private readonly Dictionary<int, Usuario> _porId = [];

    public List<Usuario> Agregados { get; } = [];

    public HashSet<string> CorreosExistentes { get; } = new(StringComparer.Ordinal);

    public int Superusuarios { get; set; }

    public void Registrar(int id, Usuario usuario) => _porId[id] = usuario;

    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.Values.FirstOrDefault(u => u.Correo == correo));

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken) =>
        Task.FromResult(CorreosExistentes.Contains(correo));

    public Task<int> ContarSuperusuariosAsync(CancellationToken cancellationToken) => Task.FromResult(Superusuarios);

    public void Agregar(Usuario usuario) => Agregados.Add(usuario);
}

internal sealed class HasherContrasenasFalso : IHasherContrasenas
{
    public VerificacionContrasena Resultado { get; set; } = VerificacionContrasena.Correcta;

    public string Hasheada { get; set; } = "hash-falso";

    public List<string> Hasheados { get; } = [];

    public string Hashear(string contrasena)
    {
        Hasheados.Add(contrasena);
        return Hasheada;
    }

    public VerificacionContrasena Verificar(string verificador, string contrasena) => Resultado;
}

internal sealed class GeneradorContrasenasFalso(string temporal) : IGeneradorContrasenas
{
    public string GenerarTemporal() => temporal;
}

internal sealed class LdapAutenticadorFalso(ResultadoLdap resultado) : ILdapAutenticador
{
    public int Llamadas { get; private set; }

    public Task<ResultadoLdap> AutenticarAsync(string correo, string contrasena, CancellationToken cancellationToken)
    {
        Llamadas++;
        return Task.FromResult(resultado);
    }
}

internal sealed class EmisorTokensFalso : IEmisorTokens
{
    public TokenEmitido Emitir(Usuario usuario) => new("token-falso", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}

internal sealed class AmbitosInstitucionalesFalso : IAmbitosInstitucionales
{
    public HashSet<int> AreasActivas { get; } = [];

    public HashSet<int> EntidadesActivas { get; } = [];

    public Task<bool> AreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(AreasActivas.Contains(areaAcademicaId));

    public Task<bool> EntidadAcademicaActivaAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(EntidadesActivas.Contains(entidadAcademicaId));

    public Task<IReadOnlyDictionary<int, AreaAcademicaResumen>> ObtenerAreasAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, AreaAcademicaResumen>>(new Dictionary<int, AreaAcademicaResumen>());

    public Task<IReadOnlyDictionary<int, EntidadAcademicaResumen>> ObtenerEntidadesAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, EntidadAcademicaResumen>>(new Dictionary<int, EntidadAcademicaResumen>());

    public Task<IReadOnlyCollection<int>> ObtenerEntidadesDeAreaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<int>>([]);
}

internal sealed class CurrentUserFalso(int id, Rol rol = Rol.Superusuario) : ICurrentUser
{
    public int Id => id;

    public Rol Rol => rol;

    public int? AreaAcademicaId => null;

    public int? EntidadAcademicaId => null;
}
