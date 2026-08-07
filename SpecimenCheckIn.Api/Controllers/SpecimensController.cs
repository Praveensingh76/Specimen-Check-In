using System;
using System.Collections.Generic;
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
    }
}
