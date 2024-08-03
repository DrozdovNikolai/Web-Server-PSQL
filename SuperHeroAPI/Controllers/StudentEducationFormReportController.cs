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
    public class StudentEducationFormReportController : ControllerBase
    {
        private readonly DataContext _context;

        public StudentEducationFormReportController(DataContext context)
        {
            _context = context;
        }

        // GET: api/StudentEducationFormReport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentEducationFormReport>>> GetStudentEducationFormReports()
        {
          if (_context.StudentEducationFormReports == null)
          {
              return NotFound();
          }
            return await _context.StudentEducationFormReports.ToListAsync();
        }

        // GET: api/StudentEducationFormReport/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentEducationFormReport>> GetStudentEducationFormReport(int id)
        {
          if (_context.StudentEducationFormReports == null)
          {
              return NotFound();
          }
            var studentEducationFormReport = await _context.StudentEducationFormReports.FindAsync(id);

            if (studentEducationFormReport == null)
            {
                return NotFound();
            }

            return studentEducationFormReport;
        }

        // PUT: api/StudentEducationFormReport/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudentEducationFormReport(int id, StudentEducationFormReport studentEducationFormReport)
        {
            if (id != studentEducationFormReport.ReportId)
            {
                return BadRequest();
            }

            _context.Entry(studentEducationFormReport).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentEducationFormReportExists(id))
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

        // POST: api/StudentEducationFormReport
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<StudentEducationFormReport>> PostStudentEducationFormReport(StudentEducationFormReport studentEducationFormReport)
        {
          if (_context.StudentEducationFormReports == null)
          {
              return Problem("Entity set 'DataContext.StudentEducationFormReports'  is null.");
          }
            _context.StudentEducationFormReports.Add(studentEducationFormReport);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStudentEducationFormReport", new { id = studentEducationFormReport.ReportId }, studentEducationFormReport);
        }

        // DELETE: api/StudentEducationFormReport/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudentEducationFormReport(int id)
        {
            if (_context.StudentEducationFormReports == null)
            {
                return NotFound();
            }
            var studentEducationFormReport = await _context.StudentEducationFormReports.FindAsync(id);
            if (studentEducationFormReport == null)
            {
                return NotFound();
            }

            _context.StudentEducationFormReports.Remove(studentEducationFormReport);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StudentEducationFormReportExists(int id)
        {
            return (_context.StudentEducationFormReports?.Any(e => e.ReportId == id)).GetValueOrDefault();
        }
    }
}
