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
    public class LWishDayController : ControllerBase
    {
        private readonly DataContext _context;

        public LWishDayController(DataContext context)
        {
            _context = context;
        }

        // GET: api/LWishDay
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LWishDay>>> GetLWishDay()
        {
          if (_context.LWishDay == null)
          {
              return NotFound();
          }
            return await _context.LWishDay.ToListAsync();
        }

        // GET: api/LWishDay/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LWishDay>> GetLWishDay(int id)
        {
          if (_context.LWishDay == null)
          {
              return NotFound();
          }
            var lWishDay = await _context.LWishDay.FindAsync(id);

            if (lWishDay == null)
            {
                return NotFound();
            }

            return lWishDay;
        }

        // PUT: api/LWishDay/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLWishDay(int id, LWishDay lWishDay)
        {
            if (id != lWishDay.LWishDayId)
            {
                return BadRequest();
            }

            _context.Entry(lWishDay).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LWishDayExists(id))
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

        // POST: api/LWishDay
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<LWishDay>> PostLWishDay(LWishDay lWishDay)
        {
          if (_context.LWishDay == null)
          {
              return Problem("Entity set 'DataContext.LWishDay'  is null.");
          }
            _context.LWishDay.Add(lWishDay);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLWishDay", new { id = lWishDay.LWishDayId }, lWishDay);
        }

        // DELETE: api/LWishDay/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLWishDay(int id)
        {
            if (_context.LWishDay == null)
            {
                return NotFound();
            }
            var lWishDay = await _context.LWishDay.FindAsync(id);
            if (lWishDay == null)
            {
                return NotFound();
            }

            _context.LWishDay.Remove(lWishDay);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LWishDayExists(int id)
        {
            return (_context.LWishDay?.Any(e => e.LWishDayId == id)).GetValueOrDefault();
        }
    }
}
