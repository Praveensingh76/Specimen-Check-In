using System;

namespace SpecimenCheckIn.Api.Services
{
    public interface ITenantProvider
    {
        Guid TenantId { get; }
        void SetTenantId(Guid tenantId);
    }
}
