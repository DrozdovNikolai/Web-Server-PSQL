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
    public class TempOfoVoController : ControllerBase
    {
        private readonly DataContext _context;

        public TempOfoVoController(DataContext context)
        {
            _context = context;
        }

        // GET: api/TempOfoVo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TempOfoVo>>> GetTempOfoVos()
        {
          if (_context.TempOfoVos == null)
          {
              return NotFound();
          }
            return await _context.TempOfoVos.ToListAsync();
        }

        // GET: api/TempOfoVo/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TempOfoVo>> GetTempOfoVo(int id)
        {
          if (_context.TempOfoVos == null)
          {
              return NotFound();
          }
            var tempOfoVo = await _context.TempOfoVos.FindAsync(id);

            if (tempOfoVo == null)
            {
                return NotFound();
            }

            return tempOfoVo;
        }

        // PUT: api/TempOfoVo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTempOfoVo(int id, TempOfoVo tempOfoVo)
        {
            if (id != tempOfoVo.Id)
            {
                return BadRequest();
            }

            _context.Entry(tempOfoVo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TempOfoVoExists(id))
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

        // POST: api/TempOfoVo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TempOfoVo>> PostTempOfoVo(TempOfoVo tempOfoVo)
        {
          if (_context.TempOfoVos == null)
          {
              return Problem("Entity set 'DataContext.TempOfoVos'  is null.");
          }
            _context.TempOfoVos.Add(tempOfoVo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTempOfoVo", new { id = tempOfoVo.Id }, tempOfoVo);
        }

        // DELETE: api/TempOfoVo/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTempOfoVo(int id)
        {
            if (_context.TempOfoVos == null)
            {
                return NotFound();
            }
            var tempOfoVo = await _context.TempOfoVos.FindAsync(id);
            if (tempOfoVo == null)
            {
                return NotFound();
            }

            _context.TempOfoVos.Remove(tempOfoVo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TempOfoVoExists(int id)
        {
            return (_context.TempOfoVos?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
