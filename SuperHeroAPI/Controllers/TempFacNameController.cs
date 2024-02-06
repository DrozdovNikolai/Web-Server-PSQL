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
    public class TempFacNameController : ControllerBase
    {
        private readonly DataContext _context;

        public TempFacNameController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempFacName
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempFacName>>> GetTempFacName()
        {
          if (_context.TempFacName == null)
          {
              return NotFound();
          }
            return await _context.TempFacName.ToListAsync();
        }

        // GET: api/TempFacName/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempFacName>> GetTempFacName(int id)
        {
          if (_context.TempFacName == null)
          {
              return NotFound();
          }
            var tempFacName = await _context.TempFacName.FindAsync(id);

            if (tempFacName == null)
            {
                return NotFound();
            }

            return tempFacName;
        }

        // PUT: api/TempFacName/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempFacName(int id, TempFacName tempFacName)
        {
            if (id != tempFacName.FacId)
            {
                return BadRequest();
            }

            _context.Entry(tempFacName).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempFacNameExists(id))
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

        // POST: api/TempFacName
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempFacName>> PostTempFacName(TempFacName tempFacName)
        {
          if (_context.TempFacName == null)
          {
              return Problem("Entity set 'DataContext.TempFacName'  is null.");
          }
            _context.TempFacName.Add(tempFacName);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempFacName", new { id = tempFacName.FacId }, tempFacName);
        }

        // DELETE: api/TempFacName/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempFacName(int id)
        {
            if (_context.TempFacName == null)
            {
                return NotFound();
            }
            var tempFacName = await _context.TempFacName.FindAsync(id);
            if (tempFacName == null)
            {
                return NotFound();
            }

            _context.TempFacName.Remove(tempFacName);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempFacNameExists(int id)
        {
            return (_context.TempFacName?.Any(e => e.FacId == id)).GetValueOrDefault();
        }
    }
}
