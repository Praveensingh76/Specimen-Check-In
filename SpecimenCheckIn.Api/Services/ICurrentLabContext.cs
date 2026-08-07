using System;

namespace SpecimenCheckIn.Api.Services
{
    public interface ICurrentLabContext
    {
        Guid LabId { get; }
        void SetLabId(Guid labId);
    }
}
