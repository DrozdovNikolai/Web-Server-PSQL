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
    public class TempItogVoController : ControllerBase
    {
        private readonly DataContext _context;

        public TempItogVoController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempItogVo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempItogVo>>> GetTempItogVo()
        {
          if (_context.TempItogVo == null)
          {
              return NotFound();
          }
            return await _context.TempItogVo.ToListAsync();
        }

        // GET: api/TempItogVo/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempItogVo>> GetTempItogVo(int id)
        {
          if (_context.TempItogVo == null)
          {
              return NotFound();
          }
            var tempItogVo = await _context.TempItogVo.FindAsync(id);

            if (tempItogVo == null)
            {
                return NotFound();
            }

            return tempItogVo;
        }

        // PUT: api/TempItogVo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempItogVo(int id, TempItogVo tempItogVo)
        {
            if (id != tempItogVo.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempItogVo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempItogVoExists(id))
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

        // POST: api/TempItogVo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempItogVo>> PostTempItogVo(TempItogVo tempItogVo)
        {
          if (_context.TempItogVo == null)
          {
              return Problem("Entity set 'DataContext.TempItogVo'  is null.");
          }
            _context.TempItogVo.Add(tempItogVo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempItogVo", new { id = tempItogVo.Id }, tempItogVo);
        }

        // DELETE: api/TempItogVo/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempItogVo(int id)
        {
            if (_context.TempItogVo == null)
            {
                return NotFound();
            }
            var tempItogVo = await _context.TempItogVo.FindAsync(id);
            if (tempItogVo == null)
            {
                return NotFound();
            }

            _context.TempItogVo.Remove(tempItogVo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempItogVoExists(int id)
        {
            return (_context.TempItogVo?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
