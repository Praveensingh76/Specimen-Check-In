using System;
using Microsoft.AspNetCore.Http;

namespace SpecimenCheckIn.Api.Services
{
    public class CurrentLabContext : ICurrentLabContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Guid? _labId;

        public CurrentLabContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid LabId
        {
            get
            {
                if (_labId.HasValue)
                {
                    return _labId.Value;
                }

                var context = _httpContextAccessor.HttpContext;
                if (context != null && context.Request.Headers.TryGetValue("X-Lab-Id", out var labIdStr))
                {
                    if (Guid.TryParse(labIdStr, out var parsedId))
                    {
                        _labId = parsedId;
                        return parsedId;
                    }
                }

                return Guid.Empty;
            }
        }

        public void SetLabId(Guid labId)
        {
            _labId = labId;
        }
    }
}
