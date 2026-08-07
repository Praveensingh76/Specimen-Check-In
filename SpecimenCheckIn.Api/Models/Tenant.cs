using System;

namespace SpecimenCheckIn.Api.Models
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // e.g., "CLINIC-A"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
