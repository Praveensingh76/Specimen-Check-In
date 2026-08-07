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
    public class SpecimensController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public SpecimensController(ApplicationDbContext context, ITenantProvider tenantProvider)
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
        public async Task<ActionResult<IEnumerable<Specimen>>> GetSpecimens()
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            return await _context.Specimens.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Specimen>> GetSpecimen(Guid id)
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            var specimen = await _context.Specimens.FirstOrDefaultAsync(s => s.Id == id);
            if (specimen == null)
            {
                return NotFound("Specimen not found.");
            }

            return specimen;
        }

        [HttpPost]
        public async Task<ActionResult<Specimen>> CreateSpecimen(Specimen specimen)
        {
            if (!ValidateTenant(out var tenantId))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            if (string.IsNullOrWhiteSpace(specimen.SpecimenNumber))
            {
                return BadRequest("Specimen number is required.");
            }

            if (await _context.Specimens.AnyAsync(s => s.SpecimenNumber == specimen.SpecimenNumber))
            {
                return BadRequest($"Specimen number '{specimen.SpecimenNumber}' already exists.");
            }

            var manifest = await _context.Manifests.FirstOrDefaultAsync(m => m.Id == specimen.ManifestId);
            if (manifest == null)
            {
                return BadRequest($"Manifest with ID '{specimen.ManifestId}' was not found.");
            }

            specimen.Id = Guid.NewGuid();
            specimen.TenantId = tenantId;
            specimen.Status = "Pending";

            _context.Specimens.Add(specimen);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSpecimen), new { id = specimen.Id }, specimen);
        }

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(Guid id, [FromBody] CheckInRequest request)
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            var specimen = await _context.Specimens.FirstOrDefaultAsync(s => s.Id == id);
            if (specimen == null)
            {
                return NotFound("Specimen not found.");
            }

            specimen.Status = "CheckedIn";
            specimen.ReceivedDate = DateTime.UtcNow;
            specimen.CheckedInBy = string.IsNullOrWhiteSpace(request.CheckedInBy) ? "Lab User" : request.CheckedInBy;
            specimen.RejectionReason = null; // Clear rejection reason if checking in again

            await _context.SaveChangesAsync();

            // Update Manifest status if all specimens are checked in
            await AutoUpdateManifestStatus(specimen.ManifestId);

            return Ok(specimen);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest request)
        {
            if (!ValidateTenant(out _))
            {
                return BadRequest("A valid X-Tenant-ID header is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest("Rejection reason is required.");
            }

            var specimen = await _context.Specimens.FirstOrDefaultAsync(s => s.Id == id);
            if (specimen == null)
            {
                return NotFound("Specimen not found.");
            }

            specimen.Status = "Rejected";
            specimen.ReceivedDate = DateTime.UtcNow;
            specimen.RejectionReason = request.Reason;
            specimen.CheckedInBy = string.IsNullOrWhiteSpace(request.CheckedInBy) ? "Lab User" : request.CheckedInBy;

            await _context.SaveChangesAsync();

            // Update Manifest status if all specimens are checked in/rejected
            await AutoUpdateManifestStatus(specimen.ManifestId);

            return Ok(specimen);
        }

        private async Task AutoUpdateManifestStatus(Guid manifestId)
        {
            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .FirstOrDefaultAsync(m => m.Id == manifestId);

            if (manifest != null && manifest.Specimens.Any())
            {
                bool allProcessed = manifest.Specimens.All(s => s.Status == "CheckedIn" || s.Status == "Rejected");
                bool anyProcessed = manifest.Specimens.Any(s => s.Status == "CheckedIn" || s.Status == "Rejected");

                if (allProcessed)
                {
                    manifest.Status = "Completed";
                }
                else if (anyProcessed)
                {
                    manifest.Status = "Received";
                }
                
                await _context.SaveChangesAsync();
            }
        }
    }

    public class CheckInRequest
    {
        public string CheckedInBy { get; set; } = string.Empty;
    }

    public class RejectRequest
    {
        public string CheckedInBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
