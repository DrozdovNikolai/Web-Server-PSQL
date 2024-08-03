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
    public class TschController : ControllerBase
    {
        private readonly DataContext _context;

        public TschController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Tsch
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tsch>>> GetTsches()
        {
          if (_context.Tsches == null)
          {
              return NotFound();
          }
            return await _context.Tsches.ToListAsync();
        }

        // GET: api/Tsch/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tsch>> GetTsch(int id)
        {
          if (_context.Tsches == null)
          {
              return NotFound();
          }
            var tsch = await _context.Tsches.FindAsync(id);

            if (tsch == null)
            {
                return NotFound();
            }

            return tsch;
        }

        // PUT: api/Tsch/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTsch(int id, Tsch tsch)
        {
            if (id != tsch.TId)
            {
                return BadRequest();
            }

            _context.Entry(tsch).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TschExists(id))
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

        // POST: api/Tsch
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Tsch>> PostTsch(Tsch tsch)
        {
          if (_context.Tsches == null)
          {
              return Problem("Entity set 'DataContext.Tsches'  is null.");
          }
            _context.Tsches.Add(tsch);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTsch", new { id = tsch.TId }, tsch);
        }

        // DELETE: api/Tsch/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTsch(int id)
        {
            if (_context.Tsches == null)
            {
                return NotFound();
            }
            var tsch = await _context.Tsches.FindAsync(id);
            if (tsch == null)
            {
                return NotFound();
            }

            _context.Tsches.Remove(tsch);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TschExists(int id)
        {
            return (_context.Tsches?.Any(e => e.TId == id)).GetValueOrDefault();
        }
    }
}
