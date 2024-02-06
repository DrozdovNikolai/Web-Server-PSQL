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
    public class TempSostavTestController : ControllerBase
    {
        private readonly DataContext _context;

        public TempSostavTestController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempSostavTest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempSostavTest>>> GetTempSostavTest()
        {
          if (_context.TempSostavTest == null)
          {
              return NotFound();
          }
            return await _context.TempSostavTest.ToListAsync();
        }

        // GET: api/TempSostavTest/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempSostavTest>> GetTempSostavTest(int id)
        {
          if (_context.TempSostavTest == null)
          {
              return NotFound();
          }
            var tempSostavTest = await _context.TempSostavTest.FindAsync(id);

            if (tempSostavTest == null)
            {
                return NotFound();
            }

            return tempSostavTest;
        }

        // PUT: api/TempSostavTest/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempSostavTest(int id, TempSostavTest tempSostavTest)
        {
            if (id != tempSostavTest.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempSostavTest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempSostavTestExists(id))
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

        // POST: api/TempSostavTest
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempSostavTest>> PostTempSostavTest(TempSostavTest tempSostavTest)
        {
          if (_context.TempSostavTest == null)
          {
              return Problem("Entity set 'DataContext.TempSostavTest'  is null.");
          }
            _context.TempSostavTest.Add(tempSostavTest);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempSostavTest", new { id = tempSostavTest.Id }, tempSostavTest);
        }

        // DELETE: api/TempSostavTest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempSostavTest(int id)
        {
            if (_context.TempSostavTest == null)
            {
                return NotFound();
            }
            var tempSostavTest = await _context.TempSostavTest.FindAsync(id);
            if (tempSostavTest == null)
            {
                return NotFound();
            }

            _context.TempSostavTest.Remove(tempSostavTest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempSostavTestExists(int id)
        {
            return (_context.TempSostavTest?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
