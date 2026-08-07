using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpecimenCheckIn.Api.Data;
using SpecimenCheckIn.Api.Models;
using SpecimenCheckIn.Api.Services;

namespace SpecimenCheckIn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManifestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public ManifestsController(ApplicationDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        private bool ValidateTenant(out Guid tenantId)
        {
            tenantId = _tenantProvider.TenantId;
            return tenantId != Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Manifest>>> GetManifests()
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            // Global Query Filters automatically scope this to the current tenant
            return await _context.Manifests
                .Include(m => m.Specimens)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Manifest>> GetManifest(Guid id)
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manifest == null)
            {
                return NotFound("Manifest not found under the active tenant.");
            }

            return manifest;
        }

        [HttpPost]
        public async Task<ActionResult<Manifest>> CreateManifest(Manifest manifest)
        {
            if (!ValidateTenant(out var tenantId))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            if (string.IsNullOrWhiteSpace(manifest.ManifestNumber))
            {
                return BadRequest("Manifest number is required.");
            }

            if (await _context.Manifests.AnyAsync(m => m.ManifestNumber == manifest.ManifestNumber))
            {
                return BadRequest($"Manifest number '{manifest.ManifestNumber}' is already in use.");
            }

            manifest.Id = Guid.NewGuid();
            manifest.TenantId = tenantId;
            manifest.CreatedAt = DateTime.UtcNow;
            manifest.Status = "Created";

            // If specimens are submitted with the manifest, assign IDs and TenantIds
            if (manifest.Specimens != null)
            {
                foreach (var specimen in manifest.Specimens)
                {
                    specimen.Id = Guid.NewGuid();
                    specimen.ManifestId = manifest.Id;
                    specimen.TenantId = tenantId;
                    specimen.Status = "Pending";
                }
            }

            _context.Manifests.Add(manifest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetManifest), new { id = manifest.Id }, manifest);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateManifest(Guid id, Manifest manifestUpdate)
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            var manifest = await _context.Manifests.FirstOrDefaultAsync(m => m.Id == id);
            if (manifest == null)
            {
                return NotFound("Manifest not found under the active tenant.");
            }

            manifest.SenderName = manifestUpdate.SenderName;
            manifest.Status = manifestUpdate.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManifest(Guid id)
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            var manifest = await _context.Manifests.FirstOrDefaultAsync(m => m.Id == id);
            if (manifest == null)
            {
                return NotFound("Manifest not found.");
            }

            _context.Manifests.Remove(manifest);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
