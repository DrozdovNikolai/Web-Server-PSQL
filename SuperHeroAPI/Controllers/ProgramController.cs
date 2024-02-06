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
    public class ProgramController : ControllerBase
    {
        private readonly DataContext _context;

        public ProgramController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Program
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Program_u>>> GetProgram_u()
        {
          if (_context.Program_u == null)
          {
              return NotFound();
          }
            return await _context.Program_u.ToListAsync();
        }

        // GET: api/Program/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Program_u>> GetProgram_u(int id)
        {
          if (_context.Program_u == null)
          {
              return NotFound();
          }
            var program_u = await _context.Program_u.FindAsync(id);

            if (program_u == null)
            {
                return NotFound();
            }

            return program_u;
        }

        // PUT: api/Program/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProgram_u(int id, Program_u program_u)
        {
            if (id != program_u.Id)
            {
                return BadRequest();
            }

            _context.Entry(program_u).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Program_uExists(id))
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

        // POST: api/Program
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Program_u>> PostProgram_u(Program_u program_u)
        {
          if (_context.Program_u == null)
          {
              return Problem("Entity set 'DataContext.Program_u'  is null.");
          }
            _context.Program_u.Add(program_u);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProgram_u", new { id = program_u.Id }, program_u);
        }

        // DELETE: api/Program/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgram_u(int id)
        {
            if (_context.Program_u == null)
            {
                return NotFound();
            }
            var program_u = await _context.Program_u.FindAsync(id);
            if (program_u == null)
            {
                return NotFound();
            }

            _context.Program_u.Remove(program_u);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Program_uExists(int id)
        {
            return (_context.Program_u?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
