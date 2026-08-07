using System;
using System.Collections.Generic;

namespace SpecimenCheckIn.Api.Models
{
    public class Lab
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<Manifest> Manifests { get; set; } = new();
    }
}
