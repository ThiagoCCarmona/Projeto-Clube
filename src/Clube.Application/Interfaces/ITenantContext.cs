using System;

namespace Clube.Application.Interfaces;

public interface ITenantContext
{
    Guid? CurrentClubeId { get; set; }
    bool IsSuperAdmin { get; }
}
