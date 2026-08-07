using System;

namespace SpecimenCheckIn.Api.Models
{
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
