using System;

namespace Clube.Infrastructure.Data;

public class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<Guid?> _currentClubeId = new AsyncLocal<Guid?>();

    public Guid? CurrentClubeId
    {
        get => _currentClubeId.Value;
        set => _currentClubeId.Value = value;
    }

    public bool IsSuperAdmin => !CurrentClubeId.HasValue;
}
