using E_Learning_Quiz.Data;
using E_Learning_Quiz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;

namespace E_Learning_Quiz.Controllers
{
    [Route("api/scores")]
    [ApiController]
    public class ScoresController : ControllerBase
    {
        private readonly DBText _context;

        public ScoresController(DBText context)
        {
            _context = context;
        }

        // POST: api/scores
        [HttpPost]
        public async Task<ActionResult<ScoreResponseDto>> PostScore(ScoreCreateDto scoreDto)
        {
            if (_context.Scores == null)
            {
                return Problem("Entity set 'DBText.Scores' is null.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.UserId == scoreDto.UserId);
            if (!userExists)
            {
                return NotFound(new { message = "User not found" });
            }

            var quizExists = await _context.Quizzes.AnyAsync(q => q.QuizId == scoreDto.QuizId);
            if (!quizExists)
            {
                return NotFound(new { message = "Quiz not found" });
            }

            if (scoreDto.ScoreValue < 0 || scoreDto.ScoreValue > 100)
            {
                return BadRequest(new { message = "Score must be between 0 and 100" });
            }

            var score = new Scores
            {
                UserId = scoreDto.UserId,
                QuizId = scoreDto.QuizId,
                ScoreValue = scoreDto.ScoreValue,
                TakenAt = DateTime.Now
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            var response = new ScoreResponseDto
            {
                ScoreId = score.ScoreId,
                UserId = score.UserId,
                QuizId = score.QuizId,
                ScoreValue = score.ScoreValue,
                TakenAt = score.TakenAt
            };

            return Ok(new
            {
                message = "Score saved successfully",
                data = response
            });
        }

        // GET: api/scores/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ScoreDetailDto>>> GetScoresByUser(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                return NotFound(new { message = "User not found" });
            }

            var scores = await _context.Scores
                .Where(s => s.UserId == userId)
                .Select(s => new ScoreDetailDto
                {
                    ScoreId = s.ScoreId,
                    UserId = s.UserId,
                    QuizId = s.QuizId,
                    ScoreValue = s.ScoreValue,
                    TakenAt = s.TakenAt,
                    QuizTitle = _context.Quizzes
                        .Where(q => q.QuizId == s.QuizId)
                        .Select(q => q.Title)
                        .FirstOrDefault(),
                    ModuleTitle = _context.Quizzes
                        .Where(q => q.QuizId == s.QuizId)
                        .Join(_context.Modules,
                            quiz => quiz.ModuleId,
                            module => module.ModuleId,
                            (quiz, module) => module.Title)
                        .FirstOrDefault(),
                    CourseTitle = _context.Quizzes
                        .Where(q => q.QuizId == s.QuizId)
                        .Join(_context.Modules,
                            quiz => quiz.ModuleId,
                            module => module.ModuleId,
                            (quiz, module) => module)
                        .Join(_context.Courses,
                            module => module.CourseId,
                            course => course.CourseId,
                            (module, course) => course.Title)
                        .FirstOrDefault()
                })
                .OrderByDescending(s => s.TakenAt)
                .ToListAsync();

            return Ok(scores);
        }

        // GET: api/scores/average
        [HttpGet("average")]
        public async Task<ActionResult<IEnumerable<AverageScoreDto>>> GetAverageScores()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var averageScores = new List<AverageScoreDto>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "sp_GetAverageScorePerStudent";
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var avgScore = new AverageScoreDto
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                TotalQuizzesTaken = reader.GetInt32(reader.GetOrdinal("TotalQuizzesTaken")),
                                AverageScore = reader.IsDBNull(reader.GetOrdinal("AverageScore"))
                                    ? 0
                                    : reader.GetDecimal(reader.GetOrdinal("AverageScore")),
                                MinScore = reader.IsDBNull(reader.GetOrdinal("MinScore"))
                                    ? 0
                                    : reader.GetDecimal(reader.GetOrdinal("MinScore")),
                                MaxScore = reader.IsDBNull(reader.GetOrdinal("MaxScore"))
                                    ? 0
                                    : reader.GetDecimal(reader.GetOrdinal("MaxScore"))
                            };
                            averageScores.Add(avgScore);
                        }
                    }
                }

                await connection.CloseAsync();
                return Ok(averageScores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving average scores", error = ex.Message });
            }
        }

        // GET: api/scores/export/pdf
        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportToPdf()
        {
            try
            {
                var scores = await _context.Scores
                    .Select(s => new
                    {
                        UserName = _context.Users.Where(u => u.UserId == s.UserId).Select(u => u.FullName).FirstOrDefault(),
                        QuizTitle = _context.Quizzes.Where(q => q.QuizId == s.QuizId).Select(q => q.Title).FirstOrDefault(),
                        s.ScoreValue,
                        s.TakenAt
                    })
                    .OrderByDescending(s => s.TakenAt)
                    .ToListAsync();

                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();

                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph("Score Report", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    document.Add(title);

                    Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    Paragraph date = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", dateFont);
                    date.Alignment = Element.ALIGN_CENTER;
                    date.SpacingAfter = 20;
                    document.Add(date);

                    PdfPTable table = new PdfPTable(4);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 3, 3, 2, 3 });

                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    PdfPCell cell = new PdfPCell(new Phrase("Student Name", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Quiz Title", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Score", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Date Taken", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    foreach (var score in scores)
                    {
                        table.AddCell(new Phrase(score.UserName ?? "N/A", dataFont));
                        table.AddCell(new Phrase(score.QuizTitle ?? "N/A", dataFont));
                        table.AddCell(new Phrase(score.ScoreValue.ToString("F2"), dataFont));
                        table.AddCell(new Phrase(score.TakenAt.ToString("yyyy-MM-dd HH:mm"), dataFont));
                    }

                    document.Add(table);
                    document.Close();
                    writer.Close();

                    byte[] bytes = ms.ToArray();
                    return File(bytes, "application/pdf", $"ScoreReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating PDF", error = ex.Message });
            }
        }

        // GET: api/scores/export/excel
        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                var scores = await _context.Scores
                    .Select(s => new
                    {
                        UserName = _context.Users.Where(u => u.UserId == s.UserId).Select(u => u.FullName).FirstOrDefault(),
                        Email = _context.Users.Where(u => u.UserId == s.UserId).Select(u => u.Email).FirstOrDefault(),
                        QuizTitle = _context.Quizzes.Where(q => q.QuizId == s.QuizId).Select(q => q.Title).FirstOrDefault(),
                        s.ScoreValue,
                        s.TakenAt
                    })
                    .OrderByDescending(s => s.TakenAt)
                    .ToListAsync();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Scores");

                    worksheet.Cells[1, 1].Value = "Student Name";
                    worksheet.Cells[1, 2].Value = "Email";
                    worksheet.Cells[1, 3].Value = "Quiz Title";
                    worksheet.Cells[1, 4].Value = "Score";
                    worksheet.Cells[1, 5].Value = "Date Taken";

                    using (var range = worksheet.Cells[1, 1, 1, 5])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    int row = 2;
                    foreach (var score in scores)
                    {
                        worksheet.Cells[row, 1].Value = score.UserName ?? "N/A";
                        worksheet.Cells[row, 2].Value = score.Email ?? "N/A";
                        worksheet.Cells[row, 3].Value = score.QuizTitle ?? "N/A";
                        worksheet.Cells[row, 4].Value = score.ScoreValue;
                        worksheet.Cells[row, 5].Value = score.TakenAt.ToString("yyyy-MM-dd HH:mm:ss");
                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"ScoreReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating Excel", error = ex.Message });
            }
        }
    }

    // DTOs tetap sama...
    public class ScoreCreateDto
    {
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Quiz ID is required")]
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Score value is required")]
        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
        public decimal ScoreValue { get; set; }
    }

    public class ScoreResponseDto
    {
        public int ScoreId { get; set; }
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public decimal ScoreValue { get; set; }
        public DateTime TakenAt { get; set; }
    }

    public class ScoreDetailDto
    {
        public int ScoreId { get; set; }
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public decimal ScoreValue { get; set; }
        public DateTime TakenAt { get; set; }
        public string QuizTitle { get; set; }
        public string ModuleTitle { get; set; }
        public string CourseTitle { get; set; }
    }

    public class AverageScoreDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int TotalQuizzesTaken { get; set; }
        public decimal AverageScore { get; set; }
        public decimal MinScore { get; set; }
        public decimal MaxScore { get; set; }
    }
}