using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreSQL.Data;
using SuperHeroAPI.md2;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly DataContext _context;

        public ContractController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Contract
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contract>>> GetContracts()
        {
          if (_context.Contracts == null)
          {
              return NotFound();
          }
            return await _context.Contracts.ToListAsync();
        }

        // GET: api/Contract/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Contract>> GetContract(int id)
        {
          if (_context.Contracts == null)
          {
              return NotFound();
          }
            var contract = await _context.Contracts.FindAsync(id);

            if (contract == null)
            {
                return NotFound();
            }

            return contract;
        }

        // PUT: api/Contract/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContract(int id, Contract contract)
        {
            if (id != contract.Id)
            {
                return BadRequest();
            }

            _context.Entry(contract).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContractExists(id))
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

        // POST: api/Contract
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Contract>> PostContract(Contract contract)
        {
          if (_context.Contracts == null)
          {
              return Problem("Entity set 'DataContext.Contracts'  is null.");
          }
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetContract", new { id = contract.Id }, contract);
        }

        // DELETE: api/Contract/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContract(int id)
        {
            if (_context.Contracts == null)
            {
                return NotFound();
            }
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContractExists(int id)
        {
            return (_context.Contracts?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private void SetParagraphStyle(WordprocessingDocument doc, string styleId, string fontName, double fontSize)
        {
            // Получаем часть стилей или создаем новую, если она не существует
            var part = doc.MainDocumentPart.StyleDefinitionsPart ?? doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            if (part.Styles == null)
            {
                part.Styles = new Styles();
                part.Styles.Save();
            }

            // Проверяем, есть ли уже такой стиль
            var style = part.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);
            if (style == null)
            {
                // Создаем новый стиль
                style = new Style() { Type = StyleValues.Paragraph, StyleId = styleId };
                style.Append(new StyleName() { Val = styleId });
                style.Append(new BasedOn() { Val = "Normal" });
                style.Append(new NextParagraphStyle() { Val = "Normal" });

                part.Styles.Append(style);
            }

            // Устанавливаем параметры шрифта
            style.Append(new StyleRunProperties(new RunFonts() { Ascii = fontName, HighAnsi = fontName }, new FontSize() { Val = (fontSize * 2).ToString() }));

            part.Styles.Save();
        }

      
        [HttpGet("Export")]
        public async Task<IActionResult> ExportContracts()
        {
            if (_context.Contracts == null)
            {
                return NotFound("No contracts found.");
            }

            var contracts = await _context.Contracts.ToListAsync();
             var contractDetails = await _context.Contracts
        .Include(c => c.Listener)
        .Include(c => c.Payer)
      
        .ToListAsync();
            using (var memoryStream = new MemoryStream())
            {
                // Load the template document from a file
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "12345.docx");
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(memoryStream);
                }

                using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
                {
                    var body = wordDocument.MainDocumentPart.Document.Body;
                    SetParagraphStyle(wordDocument, "ContractStyle", "Arial", 12);

                    foreach (var contract in contractDetails)
                    {
                        var contractParagraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId() { Val = "ContractStyle" }));
                        contractParagraph.Append(new Run(new Text($"Contract ID: {contract.Id}, Number: {contract.ContrNumber}, Details: {contract.CertDate}")));
                        body.AppendChild(contractParagraph);
                    }

                    wordDocument.MainDocumentPart.Document.Save();
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Contracts.docx");
            }
        }


    }
}
