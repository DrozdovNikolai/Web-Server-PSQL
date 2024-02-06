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
    public class LGroupsDayController : ControllerBase
    {
        private readonly DataContext _context;

        public LGroupsDayController(DataContext context)
        {
            _context = context;
        }

        // GET: api/LGroupsDay
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LGroupsDay>>> GetLGroupsDay()
        {
          if (_context.LGroupsDay == null)
          {
              return NotFound();
          }
            return await _context.LGroupsDay.ToListAsync();
        }

        // GET: api/LGroupsDay/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LGroupsDay>> GetLGroupsDay(int id)
        {
          if (_context.LGroupsDay == null)
          {
              return NotFound();
          }
            var lGroupsDay = await _context.LGroupsDay.FindAsync(id);

            if (lGroupsDay == null)
            {
                return NotFound();
            }

            return lGroupsDay;
        }

        // PUT: api/LGroupsDay/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLGroupsDay(int id, LGroupsDay lGroupsDay)
        {
            if (id != lGroupsDay.LGroupsDaysId)
            {
                return BadRequest();
            }

            _context.Entry(lGroupsDay).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LGroupsDayExists(id))
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

        // POST: api/LGroupsDay
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<LGroupsDay>> PostLGroupsDay(LGroupsDay lGroupsDay)
        {
          if (_context.LGroupsDay == null)
          {
              return Problem("Entity set 'DataContext.LGroupsDay'  is null.");
          }
            _context.LGroupsDay.Add(lGroupsDay);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLGroupsDay", new { id = lGroupsDay.LGroupsDaysId }, lGroupsDay);
        }

        // DELETE: api/LGroupsDay/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLGroupsDay(int id)
        {
            if (_context.LGroupsDay == null)
            {
                return NotFound();
            }
            var lGroupsDay = await _context.LGroupsDay.FindAsync(id);
            if (lGroupsDay == null)
            {
                return NotFound();
            }

            _context.LGroupsDay.Remove(lGroupsDay);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LGroupsDayExists(int id)
        {
            return (_context.LGroupsDay?.Any(e => e.LGroupsDaysId == id)).GetValueOrDefault();
        }
    }
}
