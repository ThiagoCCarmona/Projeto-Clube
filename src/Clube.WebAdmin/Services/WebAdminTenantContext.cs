using System;
using Clube.Application.Interfaces;
using Clube.Domain.Enums;

namespace Clube.WebAdmin.Services;

public class WebAdminTenantContext : ITenantContext
{
    private readonly WebAdminSession _session;

    public WebAdminTenantContext(WebAdminSession session)
    {
        _session = session;
    }

    public Guid? CurrentClubeId
    {
        get => _session.CurrentClubeId;
        set { }
    }

    public bool IsSuperAdmin => _session.Perfil == PerfilUsuario.SuperAdmin || !_session.CurrentClubeId.HasValue;
}
