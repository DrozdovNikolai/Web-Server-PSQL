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
    public class PayGraphController : ControllerBase
    {
        private readonly DataContext _context;

        public PayGraphController(DataContext context)
        {
            _context = context;
        }

        // GET: api/PayGraph
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayGraph>>> GetPayGraph()
        {
          if (_context.PayGraph == null)
          {
              return NotFound();
          }
            return await _context.PayGraph.ToListAsync();
        }

        // GET: api/PayGraph/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PayGraph>> GetPayGraph(int id)
        {
          if (_context.PayGraph == null)
          {
              return NotFound();
          }
            var payGraph = await _context.PayGraph.FindAsync(id);

            if (payGraph == null)
            {
                return NotFound();
            }

            return payGraph;
        }

        // PUT: api/PayGraph/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayGraph(int id, PayGraph payGraph)
        {
            if (id != payGraph.Id)
            {
                return BadRequest();
            }

            _context.Entry(payGraph).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PayGraphExists(id))
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

        // POST: api/PayGraph
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PayGraph>> PostPayGraph(PayGraph payGraph)
        {
          if (_context.PayGraph == null)
          {
              return Problem("Entity set 'DataContext.PayGraph'  is null.");
          }
            _context.PayGraph.Add(payGraph);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPayGraph", new { id = payGraph.Id }, payGraph);
        }

        // DELETE: api/PayGraph/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayGraph(int id)
        {
            if (_context.PayGraph == null)
            {
                return NotFound();
            }
            var payGraph = await _context.PayGraph.FindAsync(id);
            if (payGraph == null)
            {
                return NotFound();
            }

            _context.PayGraph.Remove(payGraph);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PayGraphExists(int id)
        {
            return (_context.PayGraph?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
