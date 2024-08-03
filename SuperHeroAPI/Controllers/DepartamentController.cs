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
    public class DepartamentController : ControllerBase
    {
        private readonly DataContext _context;

        public DepartamentController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Departament
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Departament>>> GetDepartaments()
        {
          if (_context.Departaments == null)
          {
              return NotFound();
          }
            return await _context.Departaments.ToListAsync();
        }

        // GET: api/Departament/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Departament>> GetDepartament(int id)
        {
          if (_context.Departaments == null)
          {
              return NotFound();
          }
            var departament = await _context.Departaments.FindAsync(id);

            if (departament == null)
            {
                return NotFound();
            }

            return departament;
        }

        // PUT: api/Departament/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDepartament(int id, Departament departament)
        {
            if (id != departament.DepId)
            {
                return BadRequest();
            }

            _context.Entry(departament).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartamentExists(id))
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

        // POST: api/Departament
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Departament>> PostDepartament(Departament departament)
        {
          if (_context.Departaments == null)
          {
              return Problem("Entity set 'DataContext.Departaments'  is null.");
          }
            _context.Departaments.Add(departament);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDepartament", new { id = departament.DepId }, departament);
        }

        // DELETE: api/Departament/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartament(int id)
        {
            if (_context.Departaments == null)
            {
                return NotFound();
            }
            var departament = await _context.Departaments.FindAsync(id);
            if (departament == null)
            {
                return NotFound();
            }

            _context.Departaments.Remove(departament);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DepartamentExists(int id)
        {
            return (_context.Departaments?.Any(e => e.DepId == id)).GetValueOrDefault();
        }
    }
}
