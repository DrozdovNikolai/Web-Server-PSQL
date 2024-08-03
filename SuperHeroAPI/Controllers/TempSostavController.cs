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
    public class TempSostavController : ControllerBase
    {
        private readonly DataContext _context;

        public TempSostavController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempSostav
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempSostav>>> GetTempSostavs()
        {
          if (_context.TempSostavs == null)
          {
              return NotFound();
          }
            return await _context.TempSostavs.ToListAsync();
        }

        // GET: api/TempSostav/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempSostav>> GetTempSostav(int id)
        {
          if (_context.TempSostavs == null)
          {
              return NotFound();
          }
            var tempSostav = await _context.TempSostavs.FindAsync(id);

            if (tempSostav == null)
            {
                return NotFound();
            }

            return tempSostav;
        }

        // PUT: api/TempSostav/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempSostav(int id, TempSostav tempSostav)
        {
            if (id != tempSostav.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempSostav).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempSostavExists(id))
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

        // POST: api/TempSostav
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempSostav>> PostTempSostav(TempSostav tempSostav)
        {
          if (_context.TempSostavs == null)
          {
              return Problem("Entity set 'DataContext.TempSostavs'  is null.");
          }
            _context.TempSostavs.Add(tempSostav);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempSostav", new { id = tempSostav.Id }, tempSostav);
        }

        // DELETE: api/TempSostav/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempSostav(int id)
        {
            if (_context.TempSostavs == null)
            {
                return NotFound();
            }
            var tempSostav = await _context.TempSostavs.FindAsync(id);
            if (tempSostav == null)
            {
                return NotFound();
            }

            _context.TempSostavs.Remove(tempSostav);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempSostavExists(int id)
        {
            return (_context.TempSostavs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
