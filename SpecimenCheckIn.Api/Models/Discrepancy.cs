using System;
using System.Text.Json.Serialization;

namespace SpecimenCheckIn.Api.Models
{
    public class Discrepancy
    {
        public Guid Id { get; set; }
        public Guid ManifestId { get; set; }
        public Guid? SpecimenId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DiscrepancyType Type { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DiscrepancyStatus Status { get; set; } = DiscrepancyStatus.Open;

        public string? Notes { get; set; }

        [JsonIgnore]
        public Manifest? Manifest { get; set; }

        [JsonIgnore]
        public Specimen? Specimen { get; set; }
    }
}
