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
        private readonly ICurrentLabContext _labContext;

        public ManifestsController(ApplicationDbContext context, ICurrentLabContext labContext)
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
        public async Task<ActionResult<IEnumerable<Manifest>>> GetManifests()
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            return await _context.Manifests
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Manifest>> GetManifest(Guid id)
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manifest == null)
            {
                return NotFound("Manifest not found under the active lab.");
            }

            return manifest;
        }

        [HttpPost]
        public async Task<ActionResult<Manifest>> CreateManifest(Manifest manifest)
        {
            if (!ValidateLab(out var labId))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Code))
            {
                return BadRequest("Manifest code is required.");
            }

            if (await _context.Manifests.AnyAsync(m => m.Code == manifest.Code))
            {
                return BadRequest($"Manifest with code '{manifest.Code}' already exists.");
            }

            manifest.Id = Guid.NewGuid();
            manifest.LabId = labId;
            manifest.SentAt = DateTime.UtcNow;
            manifest.Status = ManifestStatus.Open;

            if (manifest.Specimens != null)
            {
                foreach (var specimen in manifest.Specimens)
                {
                    specimen.Id = Guid.NewGuid();
                    specimen.ManifestId = manifest.Id;
                    specimen.Status = SpecimenStatus.Pending;
                }
            }

            _context.Manifests.Add(manifest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetManifest), new { id = manifest.Id }, manifest);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateManifest(Guid id, Manifest manifestUpdate)
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
            }

            var manifest = await _context.Manifests.FirstOrDefaultAsync(m => m.Id == id);
            if (manifest == null)
            {
                return NotFound("Manifest not found under the active lab.");
            }

            manifest.SourceClinic = manifestUpdate.SourceClinic;
            manifest.Status = manifestUpdate.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManifest(Guid id)
        {
            if (!ValidateLab(out _))
            {
                return BadRequest("A valid X-Lab-Id header is required.");
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
