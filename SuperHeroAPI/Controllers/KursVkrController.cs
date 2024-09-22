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
    public class KursVkrController : ControllerBase
    {
        private readonly DataContext _context;

        public KursVkrController(DataContext context)
        {
            _context = context;
        }

        // GET: api/KursVkr
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KursVkr>>> GetKursVkrs()
        {
          if (_context.KursVkrs == null)
          {
              return NotFound();
          }
            return await _context.KursVkrs.ToListAsync();
        }

        // GET: api/KursVkr/5
        [HttpGet("{id}")]
        public async Task<ActionResult<KursVkr>> GetKursVkr(int id)
        {
          if (_context.KursVkrs == null)
          {
              return NotFound();
          }
            var kursVkr = await _context.KursVkrs.FindAsync(id);

            if (kursVkr == null)
            {
                return NotFound();
            }

            return kursVkr;
        }

        // PUT: api/KursVkr/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutKursVkr(int id, KursVkr kursVkr)
        {
            if (id != kursVkr.Id)
            {
                return BadRequest();
            }

            _context.Entry(kursVkr).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KursVkrExists(id))
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

        // POST: api/KursVkr
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<KursVkr>> PostKursVkr(KursVkr kursVkr)
        {
          if (_context.KursVkrs == null)
          {
              return Problem("Entity set 'DataContext.KursVkrs'  is null.");
          }
            _context.KursVkrs.Add(kursVkr);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetKursVkr", new { id = kursVkr.Id }, kursVkr);
        }

        // DELETE: api/KursVkr/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKursVkr(int id)
        {
            if (_context.KursVkrs == null)
            {
                return NotFound();
            }
            var kursVkr = await _context.KursVkrs.FindAsync(id);
            if (kursVkr == null)
            {
                return NotFound();
            }

            _context.KursVkrs.Remove(kursVkr);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool KursVkrExists(int id)
        {
            return (_context.KursVkrs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
