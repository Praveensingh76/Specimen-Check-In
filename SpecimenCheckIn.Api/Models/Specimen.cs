using System;
using System.Text.Json.Serialization;

namespace SpecimenCheckIn.Api.Models
{
    public class Specimen
    {
        public Guid Id { get; set; }
        public Guid ManifestId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Patient { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SpecimenStatus Status { get; set; } = SpecimenStatus.Pending;

        public string? ReceivedBy { get; set; }
        public DateTime? ReceivedAt { get; set; }

        [JsonIgnore]
        public Manifest? Manifest { get; set; }
    }
}
