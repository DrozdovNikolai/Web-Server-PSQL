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
    public class ForSchedPracController : ControllerBase
    {
        private readonly DataContext _context;

        public ForSchedPracController(DataContext context)
        {
            _context = context;
        }

        // GET: api/ForSchedPrac
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ForSchedPrac>>> GetForSchedPrac()
        {
          if (_context.ForSchedPrac == null)
          {
              return NotFound();
          }
            return await _context.ForSchedPrac.ToListAsync();
        }

        // GET: api/ForSchedPrac/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ForSchedPrac>> GetForSchedPrac(int id)
        {
          if (_context.ForSchedPrac == null)
          {
              return NotFound();
          }
            var forSchedPrac = await _context.ForSchedPrac.FindAsync(id);

            if (forSchedPrac == null)
            {
                return NotFound();
            }

            return forSchedPrac;
        }

        // PUT: api/ForSchedPrac/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutForSchedPrac(int id, ForSchedPrac forSchedPrac)
        {
            if (id != forSchedPrac.Id)
            {
                return BadRequest();
            }

            _context.Entry(forSchedPrac).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ForSchedPracExists(id))
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

        // POST: api/ForSchedPrac
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ForSchedPrac>> PostForSchedPrac(ForSchedPrac forSchedPrac)
        {
          if (_context.ForSchedPrac == null)
          {
              return Problem("Entity set 'DataContext.ForSchedPrac'  is null.");
          }
            _context.ForSchedPrac.Add(forSchedPrac);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetForSchedPrac", new { id = forSchedPrac.Id }, forSchedPrac);
        }

        // DELETE: api/ForSchedPrac/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForSchedPrac(int id)
        {
            if (_context.ForSchedPrac == null)
            {
                return NotFound();
            }
            var forSchedPrac = await _context.ForSchedPrac.FindAsync(id);
            if (forSchedPrac == null)
            {
                return NotFound();
            }

            _context.ForSchedPrac.Remove(forSchedPrac);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ForSchedPracExists(int id)
        {
            return (_context.ForSchedPrac?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
