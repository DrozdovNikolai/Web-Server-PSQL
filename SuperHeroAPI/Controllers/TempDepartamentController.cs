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
    public class TempDepartamentController : ControllerBase
    {
        private readonly DataContext _context;

        public TempDepartamentController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempDepartament
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempDepartament>>> GetTempDepartament()
        {
          if (_context.TempDepartament == null)
          {
              return NotFound();
          }
            return await _context.TempDepartament.ToListAsync();
        }

        // GET: api/TempDepartament/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempDepartament>> GetTempDepartament(int id)
        {
          if (_context.TempDepartament == null)
          {
              return NotFound();
          }
            var tempDepartament = await _context.TempDepartament.FindAsync(id);

            if (tempDepartament == null)
            {
                return NotFound();
            }

            return tempDepartament;
        }

        // PUT: api/TempDepartament/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempDepartament(int id, TempDepartament tempDepartament)
        {
            if (id != tempDepartament.DepId)
            {
                return BadRequest();
            }

            _context.Entry(tempDepartament).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempDepartamentExists(id))
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

        // POST: api/TempDepartament
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempDepartament>> PostTempDepartament(TempDepartament tempDepartament)
        {
          if (_context.TempDepartament == null)
          {
              return Problem("Entity set 'DataContext.TempDepartament'  is null.");
          }
            _context.TempDepartament.Add(tempDepartament);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempDepartament", new { id = tempDepartament.DepId }, tempDepartament);
        }

        // DELETE: api/TempDepartament/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempDepartament(int id)
        {
            if (_context.TempDepartament == null)
            {
                return NotFound();
            }
            var tempDepartament = await _context.TempDepartament.FindAsync(id);
            if (tempDepartament == null)
            {
                return NotFound();
            }

            _context.TempDepartament.Remove(tempDepartament);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempDepartamentExists(int id)
        {
            return (_context.TempDepartament?.Any(e => e.DepId == id)).GetValueOrDefault();
        }
    }
}
