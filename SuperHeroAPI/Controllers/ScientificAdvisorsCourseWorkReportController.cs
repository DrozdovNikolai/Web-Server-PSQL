using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreSQL.Data;
using SuperHeroAPI.md2;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScientificAdvisorsCourseWorkReportController : ControllerBase
    {
        private readonly DataContext _context;

        public ScientificAdvisorsCourseWorkReportController(DataContext context)
        {
            _context = context;
        }

        // GET: api/ScientificAdvisorsCourseWorkReport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScientificAdvisorsCourseWorkReport>>> GetScientificAdvisorsCourseWorkReports()
        {
          if (_context.ScientificAdvisorsCourseWorkReports == null)
          {
              return NotFound();
          }
            return await _context.ScientificAdvisorsCourseWorkReports.ToListAsync();
        }

        // GET: api/ScientificAdvisorsCourseWorkReport/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ScientificAdvisorsCourseWorkReport>> GetScientificAdvisorsCourseWorkReport(int id)
        {
          if (_context.ScientificAdvisorsCourseWorkReports == null)
          {
              return NotFound();
          }
            var scientificAdvisorsCourseWorkReport = await _context.ScientificAdvisorsCourseWorkReports.FindAsync(id);

            if (scientificAdvisorsCourseWorkReport == null)
            {
                return NotFound();
            }

            return scientificAdvisorsCourseWorkReport;
        }

        // PUT: api/ScientificAdvisorsCourseWorkReport/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutScientificAdvisorsCourseWorkReport(int id, ScientificAdvisorsCourseWorkReport scientificAdvisorsCourseWorkReport)
        {
            if (id != scientificAdvisorsCourseWorkReport.ReportId)
            {
                return BadRequest();
            }

            _context.Entry(scientificAdvisorsCourseWorkReport).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScientificAdvisorsCourseWorkReportExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ScientificAdvisorsCourseWorkReport
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ScientificAdvisorsCourseWorkReport>> PostScientificAdvisorsCourseWorkReport(ScientificAdvisorsCourseWorkReport scientificAdvisorsCourseWorkReport)
        {
          if (_context.ScientificAdvisorsCourseWorkReports == null)
          {
              return Problem("Entity set 'DataContext.ScientificAdvisorsCourseWorkReports'  is null.");
          }
            _context.ScientificAdvisorsCourseWorkReports.Add(scientificAdvisorsCourseWorkReport);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetScientificAdvisorsCourseWorkReport", new { id = scientificAdvisorsCourseWorkReport.ReportId }, scientificAdvisorsCourseWorkReport);
        }

        // DELETE: api/ScientificAdvisorsCourseWorkReport/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteScientificAdvisorsCourseWorkReport(int id)
        {
            if (_context.ScientificAdvisorsCourseWorkReports == null)
            {
                return NotFound();
            }
            var scientificAdvisorsCourseWorkReport = await _context.ScientificAdvisorsCourseWorkReports.FindAsync(id);
            if (scientificAdvisorsCourseWorkReport == null)
            {
                return NotFound();
            }

            _context.ScientificAdvisorsCourseWorkReports.Remove(scientificAdvisorsCourseWorkReport);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ScientificAdvisorsCourseWorkReportExists(int id)
        {
            return (_context.ScientificAdvisorsCourseWorkReports?.Any(e => e.ReportId == id)).GetValueOrDefault();
        }
    }
}
