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
    public class ForSchedLecController : ControllerBase
    {
        private readonly DataContext _context;

        public ForSchedLecController(DataContext context)
        {
            _context = context;
        }

        // GET: api/ForSchedLec
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ForSchedLec>>> GetForSchedLecs()
        {
          if (_context.ForSchedLecs == null)
          {
              return NotFound();
          }
            return await _context.ForSchedLecs.ToListAsync();
        }

        // GET: api/ForSchedLec/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ForSchedLec>> GetForSchedLec(int id)
        {
          if (_context.ForSchedLecs == null)
          {
              return NotFound();
          }
            var forSchedLec = await _context.ForSchedLecs.FindAsync(id);

            if (forSchedLec == null)
            {
                return NotFound();
            }

            return forSchedLec;
        }

        // PUT: api/ForSchedLec/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutForSchedLec(int id, ForSchedLec forSchedLec)
        {
            if (id != forSchedLec.Id)
            {
                return BadRequest();
            }

            _context.Entry(forSchedLec).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ForSchedLecExists(id))
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

        // POST: api/ForSchedLec
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ForSchedLec>> PostForSchedLec(ForSchedLec forSchedLec)
        {
          if (_context.ForSchedLecs == null)
          {
              return Problem("Entity set 'DataContext.ForSchedLecs'  is null.");
          }
            _context.ForSchedLecs.Add(forSchedLec);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetForSchedLec", new { id = forSchedLec.Id }, forSchedLec);
        }

        // DELETE: api/ForSchedLec/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForSchedLec(int id)
        {
            if (_context.ForSchedLecs == null)
            {
                return NotFound();
            }
            var forSchedLec = await _context.ForSchedLecs.FindAsync(id);
            if (forSchedLec == null)
            {
                return NotFound();
            }

            _context.ForSchedLecs.Remove(forSchedLec);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ForSchedLecExists(int id)
        {
            return (_context.ForSchedLecs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
