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
    public class ListenerController : ControllerBase
    {
        private readonly DataContext _context;

        public ListenerController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Listener
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Listener>>> GetListeners()
        {
          if (_context.Listeners == null)
          {
              return NotFound();
          }
            return await _context.Listeners.ToListAsync();
        }

        // GET: api/Listener/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Listener>> GetListener(int id)
        {
          if (_context.Listeners == null)
          {
              return NotFound();
          }
            var listener = await _context.Listeners.FindAsync(id);

            if (listener == null)
            {
                return NotFound();
            }

            return listener;
        }

        // PUT: api/Listener/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutListener(int id, Listener listener)
        {
            if (id != listener.Id)
            {
                return BadRequest();
            }

            _context.Entry(listener).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ListenerExists(id))
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

        // POST: api/Listener
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Listener>> PostListener(Listener listener)
        {
          if (_context.Listeners == null)
          {
              return Problem("Entity set 'DataContext.Listeners'  is null.");
          }
            _context.Listeners.Add(listener);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetListener", new { id = listener.Id }, listener);
        }

        // DELETE: api/Listener/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteListener(int id)
        {
            if (_context.Listeners == null)
            {
                return NotFound();
            }
            var listener = await _context.Listeners.FindAsync(id);
            if (listener == null)
            {
                return NotFound();
            }

            _context.Listeners.Remove(listener);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ListenerExists(int id)
        {
            return (_context.Listeners?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
