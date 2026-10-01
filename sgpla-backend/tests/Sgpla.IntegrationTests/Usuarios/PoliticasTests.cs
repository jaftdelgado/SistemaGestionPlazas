using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.SharedKernel;

namespace Sgpla.IntegrationTests.Usuarios;

/// <summary>Registro de las políticas que declara <see cref="Politicas"/>: cada una exige su rol.</summary>
public sealed class PoliticasTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    [Theory]
    [InlineData(Politicas.Superusuario, Rol.Superusuario)]
    [InlineData(Politicas.Dgaa, Rol.Dgaa)]
    [InlineData(Politicas.EntidadAcademica, Rol.EntidadAcademica)]
    public async Task Politica_ExigeElRolQueLeCorresponde(string politica, Rol rol)
    {
        var proveedor = _api.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var registrada = await proveedor.GetPolicyAsync(politica);

        registrada.ShouldNotBeNull();
        var claimRol = registrada.Requirements.OfType<ClaimsAuthorizationRequirement>().ShouldHaveSingleItem();
        claimRol.ClaimType.ShouldBe("rol");
        claimRol.AllowedValues.ShouldBe([((byte)rol).ToString(CultureInfo.InvariantCulture)]);
        registrada.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Politica_DgaaOEntidadAcademica_ExigeAmbosRoles()
    {
        var proveedor = _api.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var registrada = await proveedor.GetPolicyAsync(Politicas.DgaaOEntidadAcademica);

        registrada.ShouldNotBeNull();
        var claimRol = registrada.Requirements.OfType<ClaimsAuthorizationRequirement>().ShouldHaveSingleItem();
        claimRol.ClaimType.ShouldBe("rol");
        claimRol.AllowedValues.ShouldBe(
        [
            ((byte)Rol.Dgaa).ToString(CultureInfo.InvariantCulture),
            ((byte)Rol.EntidadAcademica).ToString(CultureInfo.InvariantCulture),
        ]);
        registrada.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldHaveSingleItem();
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();
}
