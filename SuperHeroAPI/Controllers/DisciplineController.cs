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
    public class DisciplineController : ControllerBase
    {
        private readonly DataContext _context;

        public DisciplineController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Discipline
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Discipline>>> GetDiscipline()
        {
          if (_context.Discipline == null)
          {
              return NotFound();
          }
            return await _context.Discipline.ToListAsync();
        }

        // GET: api/Discipline/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Discipline>> GetDiscipline(int id)
        {
          if (_context.Discipline == null)
          {
              return NotFound();
          }
            var discipline = await _context.Discipline.FindAsync(id);

            if (discipline == null)
            {
                return NotFound();
            }

            return discipline;
        }

        // PUT: api/Discipline/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDiscipline(int id, Discipline discipline)
        {
            if (id != discipline.DisciplinesId)
            {
                return BadRequest();
            }

            _context.Entry(discipline).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DisciplineExists(id))
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

        // POST: api/Discipline
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Discipline>> PostDiscipline(Discipline discipline)
        {
          if (_context.Discipline == null)
          {
              return Problem("Entity set 'DataContext.Discipline'  is null.");
          }
            _context.Discipline.Add(discipline);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDiscipline", new { id = discipline.DisciplinesId }, discipline);
        }

        // DELETE: api/Discipline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscipline(int id)
        {
            if (_context.Discipline == null)
            {
                return NotFound();
            }
            var discipline = await _context.Discipline.FindAsync(id);
            if (discipline == null)
            {
                return NotFound();
            }

            _context.Discipline.Remove(discipline);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DisciplineExists(int id)
        {
            return (_context.Discipline?.Any(e => e.DisciplinesId == id)).GetValueOrDefault();
        }
    }
}
