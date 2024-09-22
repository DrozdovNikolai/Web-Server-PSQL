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
    public class Program_uController : ControllerBase
    {
        private readonly DataContext _context;

        public Program_uController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Program_u
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Program_u>>> GetPrograms()
        {
          if (_context.Programs == null)
          {
              return NotFound();
          }
            return await _context.Programs.ToListAsync();
        }

        // GET: api/Program_u/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Program_u>> GetProgram_u(int id)
        {
          if (_context.Programs == null)
          {
              return NotFound();
          }
            var program_u = await _context.Programs.FindAsync(id);

            if (program_u == null)
            {
                return NotFound();
            }

            return program_u;
        }

        // PUT: api/Program_u/5
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

        // POST: api/Program_u
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Program_u>> PostProgram_u(Program_u program_u)
        {
          if (_context.Programs == null)
          {
              return Problem("Entity set 'DataContext.Programs'  is null.");
          }
            _context.Programs.Add(program_u);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProgram_u", new { id = program_u.Id }, program_u);
        }

        // DELETE: api/Program_u/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgram_u(int id)
        {
            if (_context.Programs == null)
            {
                return NotFound();
            }
            var program_u = await _context.Programs.FindAsync(id);
            if (program_u == null)
            {
                return NotFound();
            }

            _context.Programs.Remove(program_u);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Program_uExists(int id)
        {
            return (_context.Programs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
