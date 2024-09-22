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
    public class LGroupController : ControllerBase
    {
        private readonly DataContext _context;

        public LGroupController(DataContext context)
        {
            _context = context;
        }

        // GET: api/LGroup
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LGroup>>> GetLGroups()
        {
          if (_context.LGroups == null)
          {
              return NotFound();
          }
            return await _context.LGroups.ToListAsync();
        }

        // GET: api/LGroup/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LGroup>> GetLGroup(int id)
        {
          if (_context.LGroups == null)
          {
              return NotFound();
          }
            var lGroup = await _context.LGroups.FindAsync(id);

            if (lGroup == null)
            {
                return NotFound();
            }

            return lGroup;
        }

        // PUT: api/LGroup/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLGroup(int id, LGroup lGroup)
        {
            if (id != lGroup.Id)
            {
                return BadRequest();
            }

            _context.Entry(lGroup).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LGroupExists(id))
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

        // POST: api/LGroup
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<LGroup>> PostLGroup(LGroup lGroup)
        {
          if (_context.LGroups == null)
          {
              return Problem("Entity set 'DataContext.LGroups'  is null.");
          }
            _context.LGroups.Add(lGroup);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLGroup", new { id = lGroup.Id }, lGroup);
        }

        // DELETE: api/LGroup/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLGroup(int id)
        {
            if (_context.LGroups == null)
            {
                return NotFound();
            }
            var lGroup = await _context.LGroups.FindAsync(id);
            if (lGroup == null)
            {
                return NotFound();
            }

            _context.LGroups.Remove(lGroup);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LGroupExists(int id)
        {
            return (_context.LGroups?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
