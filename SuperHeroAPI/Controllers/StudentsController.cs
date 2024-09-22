using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreSQL.Data;
using SuperHeroAPI.md2;



using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Text;
using System.Runtime.InteropServices.Marshalling;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly DataContext _context;

        public StudentsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Students
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudent()
        {
          if (_context.Students == null)
          {
              return NotFound();
          }
           return await _context.Students.AsNoTracking().Include(s => s.Group).ToListAsync();
        }

        // GET: api/Students/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
          if (_context.Students == null)
          {
              return NotFound();
          }
            var student = await _context.Students
    .AsNoTracking()
    .Include(s => s.Group)
    .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return student;
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
        public async Task<IActionResult> ExportStudents()
        {
            if (_context.Students == null)
            {
                return NotFound("No students found.");
            }

            var students = await _context.Students.Include(s => s.Group).ToListAsync();

            using (var memoryStream = new MemoryStream())
            {
                // Загружаем шаблон документа
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "123.docx");
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(memoryStream);
                }
                var groupedStudents = students.GroupBy(s => new { s.Group.GroupNumber, s.IsBudget })
                                                 .OrderBy(g => g.Key.GroupNumber)
                                                 .ThenBy(g => g.Key.IsBudget);
                using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
                {
                    var body = wordDocument.MainDocumentPart.Document.Body;
                    SetParagraphStyle(wordDocument, "MyNewStyle", "Times New Roman", 14);
                    var budgetCountParas = body.Descendants<Paragraph>().Where(p => p.InnerText.Contains("\u041a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e\u0020\u0441\u0442\u0443\u0434\u0435\u043d\u0442\u043e\u0432\u0020\u043d\u0430\u0020\u0431\u044e\u0434\u0436\u0435\u0442\u043d\u043e\u0439\u0020\u0444\u043e\u0440\u043c\u0435\u0020\u043e\u0431\u0443\u0447\u0435\u043d\u0438\u044f\u003a")).FirstOrDefault();
                    var nonBudgetCountParas = body.Descendants<Paragraph>().Where(p => p.InnerText.Contains("\u041a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e\u0020\u0441\u0442\u0443\u0434\u0435\u043d\u0442\u043e\u0432\u0020\u043d\u0430\u0020\u0434\u043e\u0433\u043e\u0432\u043e\u0440\u043d\u043e\u0439\u0020\u0444\u043e\u0440\u043c\u0435\u0020\u043e\u0431\u0443\u0447\u0435\u043d\u0438\u044f\u003a")).FirstOrDefault();

                   
                    if (budgetCountParas != null)
                    {
                        // Устанавливаем стиль параграфа
                        budgetCountParas.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "MyNewStyle" });
                        budgetCountParas.Append(new Run(new Text($"{students.Count(s => (bool)s.IsBudget)}")));
                    }
                    if (nonBudgetCountParas != null)
                    {
                        // Устанавливаем стиль параграфа
                        nonBudgetCountParas.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "MyNewStyle" });
                        nonBudgetCountParas.Append(new Run(new Text($"{students.Count(s => !(bool)s.IsBudget)}")));
                    }





                    foreach (var group in groupedStudents)
                    {
                        var listStartPara = body.Descendants<Paragraph>().FirstOrDefault(p => p.InnerText.Contains($"\u0421\u043f\u0438\u0441\u043e\u043a\u0020\u0441\u0442\u0443\u0434\u0435\u043d\u0442\u043e\u0432\u0020\u043d\u0430\u0020{((bool)group.Key.IsBudget ? "\u0431\u044e\u0434\u0436\u0435\u0442\u043d\u043e\u0439" : "\u0434\u043e\u0433\u043e\u0432\u043e\u0440\u043d\u043e\u0439")} \u0444\u043e\u0440\u043c\u0435\u0020\u043e\u0431\u0443\u0447\u0435\u043d\u0438\u044f\u003a"));
                        if (listStartPara != null)
                        {
                            foreach (var student in group)
                            {
                                var studentPara = listStartPara.InsertAfterSelf(new Paragraph(new ParagraphProperties(new ParagraphStyleId() { Val = "MyNewStyle" })));
                                studentPara.AppendChild(new Run(new Text($"{student.LastName} {student.FirstName} {student.Patronymic}, {student.Group.GroupNumber} \u0433\u0440\u0443\u043f\u043f\u0430")));
                            
                             
                            }
                        }
                    }

                    wordDocument.MainDocumentPart.Document.Save();
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Updated_Students.docx");
            }
        }



        // PUT: api/Students/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, Student student)
        {
            if (id != student.StudentId)
            {
                return BadRequest();
            }

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
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

        // POST: api/Students
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Student>> PostStudent(Student student)
        {
          if (_context.Students == null)
          {
              return Problem("Entity set 'DataContext.Student'  is null.");
          }
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStudent", new { id = student.StudentId }, student);
        }

        // DELETE: api/Students/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            if (_context.Students == null)
            {
                return NotFound();
            }
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        
        private bool StudentExists(int id)
        {
            return (_context.Students?.Any(e => e.StudentId == id)).GetValueOrDefault();
        }
    }
}
