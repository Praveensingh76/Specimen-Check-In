using System;
using System.Collections.Generic;

namespace SpecimenCheckIn.Api.Models
{
    public class Manifest : ITenantEntity
    {
        public Guid Id { get; set; }
        public string ManifestNumber { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Status { get; set; } = "Created"; // "Created", "Received", "Completed"
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Specimen> Specimens { get; set; } = new();
    }
}
