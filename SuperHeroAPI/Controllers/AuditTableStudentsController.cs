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
    public class AuditTableStudentsController : ControllerBase
    {
        private readonly DataContext _context;

        public AuditTableStudentsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/AuditTableStudents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditTableStudent>>> GetAuditTableStudent()
        {
          if (_context.AuditTableStudent == null)
          {
              return NotFound();
          }
            return await _context.AuditTableStudent.ToListAsync();
        }

        // GET: api/AuditTableStudents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditTableStudent>> GetAuditTableStudent(int id)
        {
          if (_context.AuditTableStudent == null)
          {
              return NotFound();
          }
            var auditTableStudent = await _context.AuditTableStudent.FindAsync(id);

            if (auditTableStudent == null)
            {
                return NotFound();
            }

            return auditTableStudent;
        }

        // PUT: api/AuditTableStudents/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuditTableStudent(int id, AuditTableStudent auditTableStudent)
        {
            if (id != auditTableStudent.AuditId)
            {
                return BadRequest();
            }

            _context.Entry(auditTableStudent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuditTableStudentExists(id))
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

        // POST: api/AuditTableStudents
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AuditTableStudent>> PostAuditTableStudent(AuditTableStudent auditTableStudent)
        {
          if (_context.AuditTableStudent == null)
          {
              return Problem("Entity set 'DataContext.AuditTableStudent'  is null.");
          }
            _context.AuditTableStudent.Add(auditTableStudent);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAuditTableStudent", new { id = auditTableStudent.AuditId }, auditTableStudent);
        }

        // DELETE: api/AuditTableStudents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditTableStudent(int id)
        {
            if (_context.AuditTableStudent == null)
            {
                return NotFound();
            }
            var auditTableStudent = await _context.AuditTableStudent.FindAsync(id);
            if (auditTableStudent == null)
            {
                return NotFound();
            }

            _context.AuditTableStudent.Remove(auditTableStudent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AuditTableStudentExists(int id)
        {
            return (_context.AuditTableStudent?.Any(e => e.AuditId == id)).GetValueOrDefault();
        }
    }
}
