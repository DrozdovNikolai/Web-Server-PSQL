using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (_context.CourseWorks == null)
            {
                return NotFound();
            }
            return await _context.CourseWorks.ToListAsync();
        }

        // GET: api/CourseWork/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseWork>> GetCourseWork(int id)
        {
            if (_context.CourseWorks == null)
            {
                return NotFound();
            }
            var courseWork = await _context.CourseWorks.FindAsync(id);

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
            if (_context.CourseWorks == null)
            {
                return Problem("Entity set 'DataContext.CourseWork'  is null.");
            }
            _context.CourseWorks.Add(courseWork);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCourseWork", new { id = courseWork.CourseWorkId }, courseWork);
        }

        // DELETE: api/CourseWork/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourseWork(int id)
        {
            if (_context.CourseWorks == null)
            {
                return NotFound();
            }
            var courseWork = await _context.CourseWorks.FindAsync(id);
            if (courseWork == null)
            {
                return NotFound();
            }

            _context.CourseWorks.Remove(courseWork);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CourseWorkExists(int id)
        {
            return (_context.CourseWorks?.Any(e => e.CourseWorkId == id)).GetValueOrDefault();
        }


        [HttpGet("ExportCourseWorks")]
        public async Task<IActionResult> ExportCourseWorks()
        {
            if (_context.CourseWorks == null)
            {
                return NotFound("No course works found.");
            }

            var courseWorks = await _context.CourseWorks
                                            .Include(cw => cw.CourseWorkStudent)
                                                .ThenInclude(s => s.Group)
                                            .Include(cw => cw.CourseWorkTeacher)
                                            .ToListAsync();

            var groupedCourseWorks = courseWorks.GroupBy(cw => new { cw.CourseWorkTeacher.LastName, cw.CourseWorkTeacher.FirstName, cw.CourseWorkTeacher.Patronymic })
                                               .OrderBy(g => g.Key.LastName)
                                               .ThenBy(g => g.Key.FirstName)
                                               .ThenBy(g => g.Key.Patronymic);

            using (var memoryStream = new MemoryStream())
            {
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "1234.docx");
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(memoryStream);
                }

                using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
                {
                    SetParagraphStyle(wordDocument, "MyNewStyle", "Times New Roman", 14);
                    var body = wordDocument.MainDocumentPart.Document.Body;

                    foreach (var group in groupedCourseWorks)
                    {
                        var teacherHeader = body.AppendChild(new Paragraph(new ParagraphProperties(new ParagraphStyleId() { Val = "MyNewStyle" })));
                        teacherHeader.AppendChild(new Run(new Text($"{group.Key.LastName} {group.Key.FirstName} {group.Key.Patronymic}:")));

                        foreach (var cw in group)
                        {
                            var courseworkDetails = body.AppendChild(new Paragraph(new ParagraphProperties(new ParagraphStyleId() { Val = "MyNewStyle" })));
                            courseworkDetails.AppendChild(new Run(new Text($"{cw.CourseWorkStudent.LastName} {cw.CourseWorkStudent.FirstName} {cw.CourseWorkStudent.Patronymic} - {cw.CourseWorkStudent.Group.GroupNumber}, {cw.CourseWorkTheme}")));
                        }
                    }

                    wordDocument.MainDocumentPart.Document.Save();
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Updated_CourseWorks.docx");
            }
        }

        private void SetParagraphStyle(WordprocessingDocument doc, string styleId, string fontName, double fontSize)
        {
            var part = doc.MainDocumentPart.StyleDefinitionsPart ?? doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            if (part.Styles == null)
            {
                part.Styles = new Styles();
                part.Styles.Save();
            }

            var style = part.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);
            if (style == null)
            {
                style = new Style() { Type = StyleValues.Paragraph, StyleId = styleId };
                style.Append(new StyleName() { Val = styleId });
                style.Append(new BasedOn() { Val = "Normal" });
                style.Append(new NextParagraphStyle() { Val = "Normal" });
                part.Styles.Append(style);
            }

            style.Append(new StyleRunProperties(new RunFonts() { Ascii = fontName, HighAnsi = fontName }, new FontSize() { Val = (fontSize * 2).ToString() }));
            part.Styles.Save();
        }

    }
}
