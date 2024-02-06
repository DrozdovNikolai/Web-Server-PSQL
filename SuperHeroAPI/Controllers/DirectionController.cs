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
    public class DirectionController : ControllerBase
    {
        private readonly DataContext _context;

        public DirectionController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Direction
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Direction>>> GetDirection()
        {
          if (_context.Direction == null)
          {
              return NotFound();
          }
            return await _context.Direction.ToListAsync();
        }

        // GET: api/Direction/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Direction>> GetDirection(int id)
        {
          if (_context.Direction == null)
          {
              return NotFound();
          }
            var direction = await _context.Direction.FindAsync(id);

            if (direction == null)
            {
                return NotFound();
            }

            return direction;
        }

        // PUT: api/Direction/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDirection(int id, Direction direction)
        {
            if (id != direction.DirId)
            {
                return BadRequest();
            }

            _context.Entry(direction).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DirectionExists(id))
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

        // POST: api/Direction
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Direction>> PostDirection(Direction direction)
        {
          if (_context.Direction == null)
          {
              return Problem("Entity set 'DataContext.Direction'  is null.");
          }
            _context.Direction.Add(direction);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDirection", new { id = direction.DirId }, direction);
        }

        // DELETE: api/Direction/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDirection(int id)
        {
            if (_context.Direction == null)
            {
                return NotFound();
            }
            var direction = await _context.Direction.FindAsync(id);
            if (direction == null)
            {
                return NotFound();
            }

            _context.Direction.Remove(direction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DirectionExists(int id)
        {
            return (_context.Direction?.Any(e => e.DirId == id)).GetValueOrDefault();
        }
    }
}
