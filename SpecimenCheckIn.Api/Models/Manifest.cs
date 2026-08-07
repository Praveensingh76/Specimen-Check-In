using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpecimenCheckIn.Api.Models
{
    public class Manifest
    {
        public Guid Id { get; set; }
        public Guid LabId { get; set; }
        public string Code { get; set; } = string.Empty;
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ManifestStatus Status { get; set; } = ManifestStatus.Open;
        
        public DateTime SentAt { get; set; }
        public string SourceClinic { get; set; } = string.Empty;

        [JsonIgnore]
        public Lab? Lab { get; set; }

        public List<Specimen> Specimens { get; set; } = new();
        public List<Discrepancy> Discrepancies { get; set; } = new();
    }
}
