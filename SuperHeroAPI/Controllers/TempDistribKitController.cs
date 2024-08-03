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
    public class TempDistribKitController : ControllerBase
    {
        private readonly DataContext _context;

        public TempDistribKitController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempDistribKit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempDistribKit>>> GetTempDistribKits()
        {
          if (_context.TempDistribKits == null)
          {
              return NotFound();
          }
            return await _context.TempDistribKits.ToListAsync();
        }

        // GET: api/TempDistribKit/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempDistribKit>> GetTempDistribKit(int id)
        {
          if (_context.TempDistribKits == null)
          {
              return NotFound();
          }
            var tempDistribKit = await _context.TempDistribKits.FindAsync(id);

            if (tempDistribKit == null)
            {
                return NotFound();
            }

            return tempDistribKit;
        }

        // PUT: api/TempDistribKit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempDistribKit(int id, TempDistribKit tempDistribKit)
        {
            if (id != tempDistribKit.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempDistribKit).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempDistribKitExists(id))
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

        // POST: api/TempDistribKit
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempDistribKit>> PostTempDistribKit(TempDistribKit tempDistribKit)
        {
          if (_context.TempDistribKits == null)
          {
              return Problem("Entity set 'DataContext.TempDistribKits'  is null.");
          }
            _context.TempDistribKits.Add(tempDistribKit);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempDistribKit", new { id = tempDistribKit.Id }, tempDistribKit);
        }

        // DELETE: api/TempDistribKit/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempDistribKit(int id)
        {
            if (_context.TempDistribKits == null)
            {
                return NotFound();
            }
            var tempDistribKit = await _context.TempDistribKits.FindAsync(id);
            if (tempDistribKit == null)
            {
                return NotFound();
            }

            _context.TempDistribKits.Remove(tempDistribKit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempDistribKitExists(int id)
        {
            return (_context.TempDistribKits?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
