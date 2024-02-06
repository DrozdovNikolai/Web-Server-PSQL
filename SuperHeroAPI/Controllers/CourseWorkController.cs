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
    public class CourseWorkController : ControllerBase
    {
        private readonly DataContext _context;

        public CourseWorkController(DataContext context)
        {
            _context = context;
        }

        // GET: api/CourseWork
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseWork>>> GetCourseWork()
        {
          if (_context.CourseWork == null)
          {
              return NotFound();
          }
            return await _context.CourseWork.ToListAsync();
        }

        // GET: api/CourseWork/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseWork>> GetCourseWork(int id)
        {
          if (_context.CourseWork == null)
          {
              return NotFound();
          }
            var courseWork = await _context.CourseWork.FindAsync(id);

            if (courseWork == null)
            {
                return NotFound();
            }

            return courseWork;
        }

        // PUT: api/CourseWork/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourseWork(int id, CourseWork courseWork)
        {
            if (id != courseWork.CourseWorkId)
            {
                return BadRequest();
            }

            _context.Entry(courseWork).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseWorkExists(id))
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

        // POST: api/CourseWork
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CourseWork>> PostCourseWork(CourseWork courseWork)
        {
          if (_context.CourseWork == null)
          {
              return Problem("Entity set 'DataContext.CourseWork'  is null.");
          }
            _context.CourseWork.Add(courseWork);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCourseWork", new { id = courseWork.CourseWorkId }, courseWork);
        }

        // DELETE: api/CourseWork/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourseWork(int id)
        {
            if (_context.CourseWork == null)
            {
                return NotFound();
            }
            var courseWork = await _context.CourseWork.FindAsync(id);
            if (courseWork == null)
            {
                return NotFound();
            }

            _context.CourseWork.Remove(courseWork);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CourseWorkExists(int id)
        {
            return (_context.CourseWork?.Any(e => e.CourseWorkId == id)).GetValueOrDefault();
        }
    }
}
