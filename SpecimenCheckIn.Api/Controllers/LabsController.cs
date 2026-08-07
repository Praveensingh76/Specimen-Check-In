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
    public class LabsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LabsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lab>>> GetLabs()
        {
            return await _context.Labs.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Lab>> CreateLab(Lab lab)
        {
            if (string.IsNullOrWhiteSpace(lab.Name))
            {
                return BadRequest("Lab name is required.");
            }

            _context.Labs.Add(lab);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLabs), new { id = lab.Id }, lab);
        }
    }
}
