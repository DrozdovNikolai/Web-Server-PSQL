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
    public class TeachscheduleController : ControllerBase
    {
        private readonly DataContext _context;

        public TeachscheduleController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Teachschedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Teachschedule>>> GetTeachschedules()
        {
          if (_context.Teachschedules == null)
          {
              return NotFound();
          }
            return await _context.Teachschedules.ToListAsync();
        }

        // GET: api/Teachschedule/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Teachschedule>> GetTeachschedule(int id)
        {
          if (_context.Teachschedules == null)
          {
              return NotFound();
          }
            var teachschedule = await _context.Teachschedules.FindAsync(id);

            if (teachschedule == null)
            {
                return NotFound();
            }

            return teachschedule;
        }

        // PUT: api/Teachschedule/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeachschedule(int id, Teachschedule teachschedule)
        {
            if (id != teachschedule.LessonId)
            {
                return BadRequest();
            }

            _context.Entry(teachschedule).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeachscheduleExists(id))
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

        // POST: api/Teachschedule
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Teachschedule>> PostTeachschedule(Teachschedule teachschedule)
        {
          if (_context.Teachschedules == null)
          {
              return Problem("Entity set 'DataContext.Teachschedules'  is null.");
          }
            _context.Teachschedules.Add(teachschedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTeachschedule", new { id = teachschedule.LessonId }, teachschedule);
        }

        // DELETE: api/Teachschedule/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeachschedule(int id)
        {
            if (_context.Teachschedules == null)
            {
                return NotFound();
            }
            var teachschedule = await _context.Teachschedules.FindAsync(id);
            if (teachschedule == null)
            {
                return NotFound();
            }

            _context.Teachschedules.Remove(teachschedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeachscheduleExists(int id)
        {
            return (_context.Teachschedules?.Any(e => e.LessonId == id)).GetValueOrDefault();
        }
    }
}
