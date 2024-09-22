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
    public class PayerController : ControllerBase
    {
        private readonly DataContext _context;

        public PayerController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Payer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payer>>> GetPayers()
        {
          if (_context.Payers == null)
          {
              return NotFound();
          }
            return await _context.Payers.ToListAsync();
        }

        // GET: api/Payer/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payer>> GetPayer(int id)
        {
          if (_context.Payers == null)
          {
              return NotFound();
          }
            var payer = await _context.Payers.FindAsync(id);

            if (payer == null)
            {
                return NotFound();
            }

            return payer;
        }

        // PUT: api/Payer/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayer(int id, Payer payer)
        {
            if (id != payer.Id)
            {
                return BadRequest();
            }

            _context.Entry(payer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PayerExists(id))
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

        // POST: api/Payer
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Payer>> PostPayer(Payer payer)
        {
          if (_context.Payers == null)
          {
              return Problem("Entity set 'DataContext.Payers'  is null.");
          }
            _context.Payers.Add(payer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPayer", new { id = payer.Id }, payer);
        }

        // DELETE: api/Payer/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayer(int id)
        {
            if (_context.Payers == null)
            {
                return NotFound();
            }
            var payer = await _context.Payers.FindAsync(id);
            if (payer == null)
            {
                return NotFound();
            }

            _context.Payers.Remove(payer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PayerExists(int id)
        {
            return (_context.Payers?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
