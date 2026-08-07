using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        private string? RequestPath => HttpContext?.Request?.Path.Value;

        private bool ValidateLab(out Guid labId, out ActionResult? problemResult)
        {
            labId = _labContext.LabId;
            if (labId == Guid.Empty)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing Active Lab Context",
                    Detail = "A valid X-Lab-Id header is required to perform operations.",
                    Instance = RequestPath
                };
                problemResult = BadRequest(problemDetails);
                return false;
            }

            problemResult = null;
            return true;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Manifest>>> GetManifests()
        {
            if (!ValidateLab(out _, out var problem))
            {
                return problem!;
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
            if (!ValidateLab(out _, out var problem))
            {
                return problem!;
            }

            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manifest == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Manifest Not Found",
                    Detail = $"Manifest with ID '{id}' was not found under the active lab context.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            return manifest;
        }

        [HttpPost]
        public async Task<ActionResult<Manifest>> CreateManifest(Manifest manifest)
        {
            if (!ValidateLab(out var labId, out var problem))
            {
                return problem!;
            }

            if (string.IsNullOrWhiteSpace(manifest.Code))
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = "Manifest code is required.",
                    Instance = RequestPath
                };
                return BadRequest(problemDetails);
            }

            if (await _context.Manifests.AnyAsync(m => m.Code == manifest.Code))
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Duplicate Manifest Code",
                    Detail = $"A manifest with code '{manifest.Code}' already exists.",
                    Instance = RequestPath
                };
                return BadRequest(problemDetails);
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

        [HttpPost("{id}/specimens/{sid}/receive")]
        public async Task<IActionResult> ReceiveSpecimen(Guid id, Guid sid, [FromBody] ReceiveSpecimenRequest request)
        {
            if (!ValidateLab(out _, out var problem))
            {
                return problem!;
            }

            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manifest == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Manifest Not Found",
                    Detail = $"Manifest with ID '{id}' was not found under the active lab.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            var specimen = manifest.Specimens.FirstOrDefault(s => s.Id == sid);
            if (specimen == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Specimen Not Found",
                    Detail = $"Specimen with ID '{sid}' was not found in Manifest '{id}'.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            // Idempotent check
            if (specimen.Status == SpecimenStatus.Received)
            {
                return Ok(specimen);
            }

            specimen.Status = SpecimenStatus.Received;
            specimen.ReceivedAt = DateTime.UtcNow;
            specimen.ReceivedBy = request.ReceivedBy;

            // Resolve any open discrepancy linked to this specimen
            var discrepancy = await _context.Discrepancies
                .FirstOrDefaultAsync(d => d.ManifestId == id && d.SpecimenId == sid && d.Status == DiscrepancyStatus.Open);
            
            if (discrepancy != null)
            {
                discrepancy.Status = DiscrepancyStatus.Resolved;
                discrepancy.Notes = (discrepancy.Notes ?? "") + $" [Auto-Resolved: Specimen received by {request.ReceivedBy} at {DateTime.UtcNow}]";
            }

            await _context.SaveChangesAsync();
            return Ok(specimen);
        }

        [HttpPost("{id}/specimens/{sid}/flag")]
        public async Task<IActionResult> FlagSpecimen(Guid id, Guid sid, [FromBody] FlagSpecimenRequest request)
        {
            if (!ValidateLab(out _, out var problem))
            {
                return problem!;
            }

            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manifest == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Manifest Not Found",
                    Detail = $"Manifest with ID '{id}' was not found under the active lab.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            var specimen = manifest.Specimens.FirstOrDefault(s => s.Id == sid);
            if (specimen == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Specimen Not Found",
                    Detail = $"Specimen with ID '{sid}' was not found in Manifest '{id}'.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            // If already flagged, return 200 Ok
            if (specimen.Status == SpecimenStatus.Flagged)
            {
                return Ok(specimen);
            }

            specimen.Status = SpecimenStatus.Flagged;
            specimen.ReceivedAt = DateTime.UtcNow;
            specimen.ReceivedBy = request.ReceivedBy;

            // Create a Missing Discrepancy (Open)
            var discrepancy = new Discrepancy
            {
                Id = Guid.NewGuid(),
                ManifestId = id,
                SpecimenId = sid,
                Type = DiscrepancyType.Missing,
                Status = DiscrepancyStatus.Open,
                Notes = request.Notes
            };

            _context.Discrepancies.Add(discrepancy);
            await _context.SaveChangesAsync();

            return Ok(specimen);
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseManifest(Guid id)
        {
            if (!ValidateLab(out _, out var problem))
            {
                return problem!;
            }

            var manifest = await _context.Manifests
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manifest == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Manifest Not Found",
                    Detail = $"Manifest with ID '{id}' was not found under the active lab.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            // Collect unresolved items
            var pendingSpecimens = manifest.Specimens.Where(s => s.Status == SpecimenStatus.Pending).ToList();
            var openDiscrepancies = manifest.Discrepancies.Where(d => d.Status == DiscrepancyStatus.Open).ToList();

            if (pendingSpecimens.Any() || openDiscrepancies.Any())
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Manifest Cannot Be Closed",
                    Detail = "The manifest has unresolved items and cannot be closed. All specimens must be processed and all discrepancies resolved.",
                    Instance = RequestPath
                };

                problemDetails.Extensions.Add("unresolvedItems", new
                {
                    pendingSpecimensCount = pendingSpecimens.Count,
                    openDiscrepanciesCount = openDiscrepancies.Count,
                    pendingSpecimens = pendingSpecimens.Select(s => new { s.Id, s.Code, s.Patient }),
                    openDiscrepancies = openDiscrepancies.Select(d => new { d.Id, d.Type, d.Notes })
                });

                return Conflict(problemDetails);
            }

            // Fully reconciled - set status to Closed
            manifest.Status = ManifestStatus.Closed;
            await _context.SaveChangesAsync();

            return Ok(manifest);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManifest(Guid id)
        {
            if (!ValidateLab(out _, out var problem))
            {
                return problem!;
            }

            var manifest = await _context.Manifests.FirstOrDefaultAsync(m => m.Id == id);
            if (manifest == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Manifest Not Found",
                    Detail = "Manifest not found.",
                    Instance = RequestPath
                };
                return NotFound(problemDetails);
            }

            _context.Manifests.Remove(manifest);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
