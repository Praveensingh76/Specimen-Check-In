using System;
using System.Text.Json.Serialization;

namespace SpecimenCheckIn.Api.Models
{
    public class Specimen : ITenantEntity
    {
        public Guid Id { get; set; }
        public Guid ManifestId { get; set; }
        public string SpecimenNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public DateTime CollectionDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending", "CheckedIn", "Rejected"
        public string? RejectionReason { get; set; }
        public Guid TenantId { get; set; }
        public string? CheckedInBy { get; set; }

        [JsonIgnore]
        public Manifest? Manifest { get; set; }
    }
}
