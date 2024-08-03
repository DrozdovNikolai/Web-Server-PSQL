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
    public class PayGraphController : ControllerBase
    {
        private readonly DataContext _context;

        public PayGraphController(DataContext context)
        {
            _context = context;
        }

        // GET: api/PayGraph
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayGraph>>> GetPayGraphs()
        {
          if (_context.PayGraphs == null)
          {
              return NotFound();
          }
            return await _context.PayGraphs.ToListAsync();
        }

        // GET: api/PayGraph/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PayGraph>> GetPayGraph(int id)
        {
          if (_context.PayGraphs == null)
          {
              return NotFound();
          }
            var payGraph = await _context.PayGraphs.FindAsync(id);

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
          if (_context.PayGraphs == null)
          {
              return Problem("Entity set 'DataContext.PayGraphs'  is null.");
          }
            _context.PayGraphs.Add(payGraph);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPayGraph", new { id = payGraph.Id }, payGraph);
        }

        // DELETE: api/PayGraph/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayGraph(int id)
        {
            if (_context.PayGraphs == null)
            {
                return NotFound();
            }
            var payGraph = await _context.PayGraphs.FindAsync(id);
            if (payGraph == null)
            {
                return NotFound();
            }

            _context.PayGraphs.Remove(payGraph);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PayGraphExists(int id)
        {
            return (_context.PayGraphs?.Any(e => e.Id == id)).GetValueOrDefault();
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
        public async Task<IActionResult> ExportPayGraphs()
        {
            if (_context.PayGraphs == null)
            {
                return NotFound("No pay graphs found.");
            }

            var payGraphs = await _context.PayGraphs.Include(s=>s.Contract).ToListAsync()
                ;
            var currentDate = DateOnly.FromDateTime(DateTime.Now);

            using (var memoryStream = new MemoryStream())
            {
                // Load the template document from a file
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "123456.docx");
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(memoryStream);
                }

                using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
                {
                    var body = wordDocument.MainDocumentPart.Document.Body;
                    SetParagraphStyle(wordDocument, "PayGraphStyle", "Arial", 12);

                    foreach (var payGraph in payGraphs)
                    {
                        var text = $"PayGraph ID: {payGraph.Id}";
                        if (payGraph.ExpirationDate.HasValue && payGraph.ExpirationDate.Value < currentDate)
                        {
                            text += " - Expired";
                        }

                        var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId() { Val = "PayGraphStyle" }));
                        paragraph.Append(new Run(new Text(text)));
                        body.AppendChild(paragraph);
                    }

                    wordDocument.MainDocumentPart.Document.Save();
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "PayGraphs.docx");
            }
        }
    }
}
