using System;

namespace Clube.Infrastructure.Data;

public interface ITenantContext
{
    Guid? CurrentClubeId { get; set; }
    bool IsSuperAdmin { get; }
}
