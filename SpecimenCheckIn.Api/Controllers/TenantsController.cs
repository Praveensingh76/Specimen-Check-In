using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpecimenCheckIn.Api.Data;
using SpecimenCheckIn.Api.Models;

namespace SpecimenCheckIn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TenantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants()
        {
            // Get all tenants, ignoring query filter since tenants table is global and not isolated
            return await _context.Tenants.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Tenant>> CreateTenant(Tenant tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant.Code) || string.IsNullOrWhiteSpace(tenant.Name))
            {
                return BadRequest("Tenant name and code are required.");
            }

            tenant.Code = tenant.Code.ToUpperInvariant();
            
            if (await _context.Tenants.AnyAsync(t => t.Code == tenant.Code))
            {
                return BadRequest($"Tenant with code '{tenant.Code}' already exists.");
            }

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTenants), new { id = tenant.Id }, tenant);
        }
    }
}
