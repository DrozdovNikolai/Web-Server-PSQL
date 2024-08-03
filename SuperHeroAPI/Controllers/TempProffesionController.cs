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
    public class TempProffesionController : ControllerBase
    {
        private readonly DataContext _context;

        public TempProffesionController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempProffesion
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempProffesion>>> GetTempProffesions()
        {
          if (_context.TempProffesions == null)
          {
              return NotFound();
          }
            return await _context.TempProffesions.ToListAsync();
        }

        // GET: api/TempProffesion/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempProffesion>> GetTempProffesion(int id)
        {
          if (_context.TempProffesions == null)
          {
              return NotFound();
          }
            var tempProffesion = await _context.TempProffesions.FindAsync(id);

            if (tempProffesion == null)
            {
                return NotFound();
            }

            return tempProffesion;
        }

        // PUT: api/TempProffesion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempProffesion(int id, TempProffesion tempProffesion)
        {
            if (id != tempProffesion.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempProffesion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempProffesionExists(id))
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

        // POST: api/TempProffesion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempProffesion>> PostTempProffesion(TempProffesion tempProffesion)
        {
          if (_context.TempProffesions == null)
          {
              return Problem("Entity set 'DataContext.TempProffesions'  is null.");
          }
            _context.TempProffesions.Add(tempProffesion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempProffesion", new { id = tempProffesion.Id }, tempProffesion);
        }

        // DELETE: api/TempProffesion/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempProffesion(int id)
        {
            if (_context.TempProffesions == null)
            {
                return NotFound();
            }
            var tempProffesion = await _context.TempProffesions.FindAsync(id);
            if (tempProffesion == null)
            {
                return NotFound();
            }

            _context.TempProffesions.Remove(tempProffesion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempProffesionExists(int id)
        {
            return (_context.TempProffesions?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
