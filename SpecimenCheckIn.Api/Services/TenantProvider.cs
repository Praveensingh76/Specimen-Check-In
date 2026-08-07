using System;
using Microsoft.AspNetCore.Http;

namespace SpecimenCheckIn.Api.Services
{
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Guid? _tenantId;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid TenantId
        {
            get
            {
                if (_tenantId.HasValue)
                {
                    return _tenantId.Value;
                }

                var context = _httpContextAccessor.HttpContext;
                if (context != null && context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdStr))
                {
                    if (Guid.TryParse(tenantIdStr, out var parsedId))
                    {
                        _tenantId = parsedId;
                        return parsedId;
                    }
                }

                return Guid.Empty;
            }
        }

        public void SetTenantId(Guid tenantId)
        {
            _tenantId = tenantId;
        }
    }
}
