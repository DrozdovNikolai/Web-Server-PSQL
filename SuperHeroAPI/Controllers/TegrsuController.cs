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
    public class TegrsuController : ControllerBase
    {
        private readonly DataContext _context;

        public TegrsuController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Tegrsu
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tegrsu>>> GetTegrsu()
        {
          if (_context.Tegrsu == null)
          {
              return NotFound();
          }
            return await _context.Tegrsu.ToListAsync();
        }

        // GET: api/Tegrsu/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tegrsu>> GetTegrsu(int id)
        {
          if (_context.Tegrsu == null)
          {
              return NotFound();
          }
            var tegrsu = await _context.Tegrsu.FindAsync(id);

            if (tegrsu == null)
            {
                return NotFound();
            }

            return tegrsu;
        }

        // PUT: api/Tegrsu/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTegrsu(int id, Tegrsu tegrsu)
        {
            if (id != tegrsu.TegrsuId)
            {
                return BadRequest();
            }

            _context.Entry(tegrsu).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TegrsuExists(id))
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

        // POST: api/Tegrsu
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Tegrsu>> PostTegrsu(Tegrsu tegrsu)
        {
          if (_context.Tegrsu == null)
          {
              return Problem("Entity set 'DataContext.Tegrsu'  is null.");
          }
            _context.Tegrsu.Add(tegrsu);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTegrsu", new { id = tegrsu.TegrsuId }, tegrsu);
        }

        // DELETE: api/Tegrsu/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTegrsu(int id)
        {
            if (_context.Tegrsu == null)
            {
                return NotFound();
            }
            var tegrsu = await _context.Tegrsu.FindAsync(id);
            if (tegrsu == null)
            {
                return NotFound();
            }

            _context.Tegrsu.Remove(tegrsu);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TegrsuExists(int id)
        {
            return (_context.Tegrsu?.Any(e => e.TegrsuId == id)).GetValueOrDefault();
        }
    }
}
