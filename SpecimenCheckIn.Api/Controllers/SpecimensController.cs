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
        private readonly ICurrentLabContext _labContext;

        public SpecimensController(ApplicationDbContext context, ICurrentLabContext labContext)
        {
            _context = context;
            _labContext = labContext;
        }

        private bool ValidateLab(out Guid labId)
        {
            labId = _labContext.LabId;
            return labId != Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Specimen>>> GetSpecimens()
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            return await _context.Specimens.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Specimen>> GetSpecimen(Guid id)
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
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
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            if (string.IsNullOrWhiteSpace(specimen.Code))
            {
                return BadRequest("Specimen code is required.");
            }

            if (await _context.Specimens.AnyAsync(s => s.Code == specimen.Code))
            {
                return BadRequest($"Specimen with code '{specimen.Code}' already exists.");
            }

            var manifest = await _context.Manifests.FirstOrDefaultAsync(m => m.Id == specimen.ManifestId);
            if (manifest == null)
            {
                return BadRequest($"Manifest with ID '{specimen.ManifestId}' was not found.");
            }

            specimen.Id = Guid.NewGuid();
            specimen.Status = SpecimenStatus.Pending;

            _context.Specimens.Add(specimen);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSpecimen), new { id = specimen.Id }, specimen);
        }

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(Guid id, [FromBody] CheckInRequest request)
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            var specimen = await _context.Specimens.FirstOrDefaultAsync(s => s.Id == id);
            if (specimen == null)
            {
                return NotFound("Specimen not found.");
            }

            specimen.Status = SpecimenStatus.Received;
            specimen.ReceivedAt = DateTime.UtcNow;
            specimen.ReceivedBy = string.IsNullOrWhiteSpace(request.ReceivedBy) ? "Lab Tech" : request.ReceivedBy;

            await _context.SaveChangesAsync();

            // Auto-update Manifest status if all specimens are checked in or resolved
            await AutoUpdateManifestStatus(specimen.ManifestId);

            return Ok(specimen);
        }

        [HttpPost("{id}/flag")]
        public async Task<IActionResult> Flag(Guid id, [FromBody] FlagRequest request)
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            var specimen = await _context.Specimens.FirstOrDefaultAsync(s => s.Id == id);
            if (specimen == null)
            {
                return NotFound("Specimen not found.");
            }

            specimen.Status = SpecimenStatus.Flagged;
            specimen.ReceivedAt = DateTime.UtcNow;
            specimen.ReceivedBy = string.IsNullOrWhiteSpace(request.ReceivedBy) ? "Lab Tech" : request.ReceivedBy;

            // Create a discrepancy for the flagged specimen
            var discrepancy = new Discrepancy
            {
                Id = Guid.NewGuid(),
                ManifestId = specimen.ManifestId,
                SpecimenId = specimen.Id,
                Type = DiscrepancyType.Missing, // or another appropriate value
                Status = DiscrepancyStatus.Open,
                Notes = request.Notes
            };

            _context.Discrepancies.Add(discrepancy);
            await _context.SaveChangesAsync();

            await AutoUpdateManifestStatus(specimen.ManifestId);

            return Ok(specimen);
        }

        private async Task AutoUpdateManifestStatus(Guid manifestId)
        {
            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .FirstOrDefaultAsync(m => m.Id == manifestId);

            if (manifest != null && manifest.Specimens.Any())
            {
                bool anyFlagged = manifest.Specimens.Any(s => s.Status == SpecimenStatus.Flagged) || 
                                  manifest.Discrepancies.Any(d => d.Status == DiscrepancyStatus.Open);
                bool allProcessed = manifest.Specimens.All(s => s.Status == SpecimenStatus.Received || s.Status == SpecimenStatus.Flagged);

                if (allProcessed)
                {
                    manifest.Status = anyFlagged ? ManifestStatus.ClosedWithDiscrepancy : ManifestStatus.Closed;
                }
                else
                {
                    manifest.Status = ManifestStatus.Open;
                }

                await _context.SaveChangesAsync();
            }
        }
    }

    public class CheckInRequest
    {
        public string ReceivedBy { get; set; } = string.Empty;
    }

    public class FlagRequest
    {
        public string ReceivedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
