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
    public class ListenerWishController : ControllerBase
    {
        private readonly DataContext _context;

        public ListenerWishController(DataContext context)
        {
            _context = context;
        }

        // GET: api/ListenerWish
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ListenerWish>>> GetListenerWishes()
        {
          if (_context.ListenerWishes == null)
          {
              return NotFound();
          }
            return await _context.ListenerWishes.ToListAsync();
        }

        // GET: api/ListenerWish/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ListenerWish>> GetListenerWish(int id)
        {
          if (_context.ListenerWishes == null)
          {
              return NotFound();
          }
            var listenerWish = await _context.ListenerWishes.FindAsync(id);

            if (listenerWish == null)
            {
                return NotFound();
            }

            return listenerWish;
        }

        // PUT: api/ListenerWish/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutListenerWish(int id, ListenerWish listenerWish)
        {
            if (id != listenerWish.WishId)
            {
                return BadRequest();
            }

            _context.Entry(listenerWish).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ListenerWishExists(id))
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

        // POST: api/ListenerWish
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ListenerWish>> PostListenerWish(ListenerWish listenerWish)
        {
          if (_context.ListenerWishes == null)
          {
              return Problem("Entity set 'DataContext.ListenerWishes'  is null.");
          }
            _context.ListenerWishes.Add(listenerWish);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetListenerWish", new { id = listenerWish.WishId }, listenerWish);
        }

        // DELETE: api/ListenerWish/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteListenerWish(int id)
        {
            if (_context.ListenerWishes == null)
            {
                return NotFound();
            }
            var listenerWish = await _context.ListenerWishes.FindAsync(id);
            if (listenerWish == null)
            {
                return NotFound();
            }

            _context.ListenerWishes.Remove(listenerWish);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ListenerWishExists(int id)
        {
            return (_context.ListenerWishes?.Any(e => e.WishId == id)).GetValueOrDefault();
        }
    }
}
