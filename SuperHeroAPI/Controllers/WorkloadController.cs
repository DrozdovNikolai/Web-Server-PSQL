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
    public class WorkloadController : ControllerBase
    {
        private readonly DataContext _context;

        public WorkloadController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Workload
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Workload>>> GetWorkloads()
        {
          if (_context.Workloads == null)
          {
              return NotFound();
          }
            return await _context.Workloads.ToListAsync();
        }

        // GET: api/Workload/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Workload>> GetWorkload(int id)
        {
          if (_context.Workloads == null)
          {
              return NotFound();
          }
            var workload = await _context.Workloads.FindAsync(id);

            if (workload == null)
            {
                return NotFound();
            }

            return workload;
        }

        // PUT: api/Workload/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWorkload(int id, Workload workload)
        {
            if (id != workload.WlId)
            {
                return BadRequest();
            }

            _context.Entry(workload).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkloadExists(id))
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

        // POST: api/Workload
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Workload>> PostWorkload(Workload workload)
        {
          if (_context.Workloads == null)
          {
              return Problem("Entity set 'DataContext.Workloads'  is null.");
          }
            _context.Workloads.Add(workload);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWorkload", new { id = workload.WlId }, workload);
        }

        // DELETE: api/Workload/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkload(int id)
        {
            if (_context.Workloads == null)
            {
                return NotFound();
            }
            var workload = await _context.Workloads.FindAsync(id);
            if (workload == null)
            {
                return NotFound();
            }

            _context.Workloads.Remove(workload);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WorkloadExists(int id)
        {
            return (_context.Workloads?.Any(e => e.WlId == id)).GetValueOrDefault();
        }
    }
}
