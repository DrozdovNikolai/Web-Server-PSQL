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
    public class TempTeachGruzController : ControllerBase
    {
        private readonly DataContext _context;

        public TempTeachGruzController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempTeachGruz
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempTeachGruz>>> GetTempTeachGruzs()
        {
          if (_context.TempTeachGruzs == null)
          {
              return NotFound();
          }
            return await _context.TempTeachGruzs.ToListAsync();
        }

        // GET: api/TempTeachGruz/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempTeachGruz>> GetTempTeachGruz(int id)
        {
          if (_context.TempTeachGruzs == null)
          {
              return NotFound();
          }
            var tempTeachGruz = await _context.TempTeachGruzs.FindAsync(id);

            if (tempTeachGruz == null)
            {
                return NotFound();
            }

            return tempTeachGruz;
        }

        // PUT: api/TempTeachGruz/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempTeachGruz(int id, TempTeachGruz tempTeachGruz)
        {
            if (id != tempTeachGruz.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempTeachGruz).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempTeachGruzExists(id))
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

        // POST: api/TempTeachGruz
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempTeachGruz>> PostTempTeachGruz(TempTeachGruz tempTeachGruz)
        {
          if (_context.TempTeachGruzs == null)
          {
              return Problem("Entity set 'DataContext.TempTeachGruzs'  is null.");
          }
            _context.TempTeachGruzs.Add(tempTeachGruz);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempTeachGruz", new { id = tempTeachGruz.Id }, tempTeachGruz);
        }

        // DELETE: api/TempTeachGruz/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempTeachGruz(int id)
        {
            if (_context.TempTeachGruzs == null)
            {
                return NotFound();
            }
            var tempTeachGruz = await _context.TempTeachGruzs.FindAsync(id);
            if (tempTeachGruz == null)
            {
                return NotFound();
            }

            _context.TempTeachGruzs.Remove(tempTeachGruz);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempTeachGruzExists(int id)
        {
            return (_context.TempTeachGruzs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
