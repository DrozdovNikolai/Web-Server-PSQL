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
    public class TeachGruzController : ControllerBase
    {
        private readonly DataContext _context;

        public TeachGruzController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TeachGruz
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeachGruz>>> GetTeachGruzs()
        {
          if (_context.TeachGruzs == null)
          {
              return NotFound();
          }
            return await _context.TeachGruzs.ToListAsync();
        }

        // GET: api/TeachGruz/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TeachGruz>> GetTeachGruz(int id)
        {
          if (_context.TeachGruzs == null)
          {
              return NotFound();
          }
            var teachGruz = await _context.TeachGruzs.FindAsync(id);

            if (teachGruz == null)
            {
                return NotFound();
            }

            return teachGruz;
        }

        // PUT: api/TeachGruz/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeachGruz(int id, TeachGruz teachGruz)
        {
            if (id != teachGruz.Id)
            {
                return BadRequest();
            }

            _context.Entry(teachGruz).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeachGruzExists(id))
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

        // POST: api/TeachGruz
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TeachGruz>> PostTeachGruz(TeachGruz teachGruz)
        {
          if (_context.TeachGruzs == null)
          {
              return Problem("Entity set 'DataContext.TeachGruzs'  is null.");
          }
            _context.TeachGruzs.Add(teachGruz);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTeachGruz", new { id = teachGruz.Id }, teachGruz);
        }

        // DELETE: api/TeachGruz/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeachGruz(int id)
        {
            if (_context.TeachGruzs == null)
            {
                return NotFound();
            }
            var teachGruz = await _context.TeachGruzs.FindAsync(id);
            if (teachGruz == null)
            {
                return NotFound();
            }

            _context.TeachGruzs.Remove(teachGruz);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeachGruzExists(int id)
        {
            return (_context.TeachGruzs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
