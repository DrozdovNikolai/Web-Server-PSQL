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
    public class TempPracticeController : ControllerBase
    {
        private readonly DataContext _context;

        public TempPracticeController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempPractice
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempPractice>>> GetTempPractices()
        {
          if (_context.TempPractices == null)
          {
              return NotFound();
          }
            return await _context.TempPractices.ToListAsync();
        }

        // GET: api/TempPractice/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempPractice>> GetTempPractice(int id)
        {
          if (_context.TempPractices == null)
          {
              return NotFound();
          }
            var tempPractice = await _context.TempPractices.FindAsync(id);

            if (tempPractice == null)
            {
                return NotFound();
            }

            return tempPractice;
        }

        // PUT: api/TempPractice/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempPractice(int id, TempPractice tempPractice)
        {
            if (id != tempPractice.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempPractice).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempPracticeExists(id))
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

        // POST: api/TempPractice
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempPractice>> PostTempPractice(TempPractice tempPractice)
        {
          if (_context.TempPractices == null)
          {
              return Problem("Entity set 'DataContext.TempPractices'  is null.");
          }
            _context.TempPractices.Add(tempPractice);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempPractice", new { id = tempPractice.Id }, tempPractice);
        }

        // DELETE: api/TempPractice/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempPractice(int id)
        {
            if (_context.TempPractices == null)
            {
                return NotFound();
            }
            var tempPractice = await _context.TempPractices.FindAsync(id);
            if (tempPractice == null)
            {
                return NotFound();
            }

            _context.TempPractices.Remove(tempPractice);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempPracticeExists(int id)
        {
            return (_context.TempPractices?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
