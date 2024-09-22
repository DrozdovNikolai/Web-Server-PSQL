using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreSQL.Data;
using SuperHeroAPI.md2;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditoriumsController : ControllerBase
    {
        private readonly DataContext _context;

        public AuditoriumsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Auditoriums
        [HttpGet]
        [Authorize(Policy = "AuditoriumsPolicy")]
        public async Task<ActionResult<IEnumerable<Auditorium>>> GetAuditoria()
        {
          if (_context.Auditoria == null)
          {
              return NotFound();
          }
            return await _context.Auditoria.ToListAsync();
        }

        // GET: api/Auditoriums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Auditorium>> GetAuditorium(int id)
        {
          if (_context.Auditoria == null)
          {
              return NotFound();
          }
            var auditorium = await _context.Auditoria.FindAsync(id);

            if (auditorium == null)
            {
                return NotFound();
            }

            return auditorium;
        }

        // PUT: api/Auditoriums/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuditorium(int id, Auditorium auditorium)
        {
            if (id != auditorium.AudId)
            {
                return BadRequest();
            }

            _context.Entry(auditorium).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuditoriumExists(id))
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

        // POST: api/Auditoriums
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Auditorium>> PostAuditorium(Auditorium auditorium)
        {
          if (_context.Auditoria == null)
          {
              return Problem("Entity set 'DataContext.Auditoria'  is null.");
          }
            _context.Auditoria.Add(auditorium);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAuditorium", new { id = auditorium.AudId }, auditorium);
        }

        // DELETE: api/Auditoriums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditorium(int id)
        {
            if (_context.Auditoria == null)
            {
                return NotFound();
            }
            var auditorium = await _context.Auditoria.FindAsync(id);
            if (auditorium == null)
            {
                return NotFound();
            }

            _context.Auditoria.Remove(auditorium);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AuditoriumExists(int id)
        {
            return (_context.Auditoria?.Any(e => e.AudId == id)).GetValueOrDefault();
        }
    }
}
