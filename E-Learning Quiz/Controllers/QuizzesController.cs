using E_Learning_Quiz.Data;
using E_Learning_Quiz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace E_Learning_Quiz.Controllers
{
    [Route("api")]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly DBText _context;

        public QuizzesController(DBText context)
        {
            _context = context;
        }

        // GET: api/modules/{moduleId}/quizzes
        [HttpGet("modules/{moduleId}/quizzes")]
        public async Task<ActionResult<IEnumerable<QuizResponseDto>>> GetQuizzesByModule(int moduleId)
        {
            var moduleExists = await _context.Modules.AnyAsync(m => m.ModuleId == moduleId);
            if (!moduleExists)
            {
                return NotFound(new { message = "Module not found" });
            }

            var quizzes = await _context.Quizzes
                .Where(q => q.ModuleId == moduleId)
                .Select(q => new QuizResponseDto
                {
                    QuizId = q.QuizId,
                    ModuleId = q.ModuleId,
                    Title = q.Title,
                    DurationMinutes = q.DurationMinutes,
                    CreatedAt = q.CreatedAt,
                    QuestionCount = _context.Questions.Count(qs => qs.QuizId == q.QuizId)
                })
                .ToListAsync();

            return Ok(quizzes);
        }

        // GET: api/quizzes/5
        [HttpGet("quizzes/{id}")]
        public async Task<ActionResult<QuizDetailDto>> GetQuiz(int id)
        {
            try
            {
                var quizIdParam = new SqlParameter("@QuizId", id);

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "sp_GetQuizDetail";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(quizIdParam);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Read Quiz Info
                        if (!await reader.ReadAsync())
                        {
                            await connection.CloseAsync();
                            return NotFound(new { message = "Quiz not found" });
                        }

                        var quizDetail = new QuizDetailDto
                        {
                            QuizId = reader.GetInt32(reader.GetOrdinal("QuizId")),
                            ModuleId = reader.GetInt32(reader.GetOrdinal("ModuleId")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            ModuleName = reader.GetString(reader.GetOrdinal("ModuleName")),
                            CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
                            Questions = new List<QuestionWithAnswersDto>()
                        };

                        // Move to second result set
                        await reader.NextResultAsync();

                        var questionDict = new Dictionary<int, QuestionWithAnswersDto>();

                        while (await reader.ReadAsync())
                        {
                            var questionId = reader.GetInt32(reader.GetOrdinal("QuestionId"));

                            // Add question if not exists
                            if (!questionDict.ContainsKey(questionId))
                            {
                                var question = new QuestionWithAnswersDto
                                {
                                    QuestionId = questionId,
                                    QuestionText = reader.GetString(reader.GetOrdinal("QuestionText")),
                                    Answers = new List<AnswerDto>()
                                };
                                questionDict[questionId] = question;
                            }

                            // Add answer if exists
                            if (!reader.IsDBNull(reader.GetOrdinal("AnswerId")))
                            {
                                var answer = new AnswerDto
                                {
                                    AnswerId = reader.GetInt32(reader.GetOrdinal("AnswerId")),
                                    AnswerText = reader.GetString(reader.GetOrdinal("AnswerText")),
                                    IsCorrect = reader.GetBoolean(reader.GetOrdinal("IsCorrect"))
                                };
                                questionDict[questionId].Answers.Add(answer);
                            }
                        }

                        quizDetail.Questions = questionDict.Values.ToList();

                        await connection.CloseAsync();
                        return Ok(quizDetail);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving quiz details", error = ex.Message });
            }
        }

        // POST: api/modules/{moduleId}/quizzes
        [HttpPost("modules/{moduleId}/quizzes")]
        public async Task<ActionResult<QuizResponseDto>> PostQuiz(int moduleId, QuizCreateDto quizDto)
        {
            if (_context.Quizzes == null)
            {
                return Problem("Entity set 'DBText.Quizzes' is null.");
            }

            var moduleExists = await _context.Modules.AnyAsync(m => m.ModuleId == moduleId);
            if (!moduleExists)
            {
                return NotFound(new { message = "Module not found" });
            }

            var quiz = new Quizzes
            {
                ModuleId = moduleId,
                Title = quizDto.Title,
                DurationMinutes = quizDto.DurationMinutes,
                CreatedAt = DateTime.Now
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            var response = new QuizResponseDto
            {
                QuizId = quiz.QuizId,
                ModuleId = quiz.ModuleId,
                Title = quiz.Title,
                DurationMinutes = quiz.DurationMinutes,
                CreatedAt = quiz.CreatedAt,
                QuestionCount = 0
            };

            return CreatedAtAction(nameof(GetQuiz), new { id = quiz.QuizId }, response);
        }

        // PUT: api/quizzes/5
        [HttpPut("quizzes/{id}")]
        public async Task<IActionResult> PutQuiz(int id, QuizUpdateDto quizDto)
        {
            var existingQuiz = await _context.Quizzes.FindAsync(id);

            if (existingQuiz == null)
            {
                return NotFound(new { message = "Quiz not found" });
            }

            if (!string.IsNullOrEmpty(quizDto.Title))
            {
                existingQuiz.Title = quizDto.Title;
            }

            if (quizDto.DurationMinutes.HasValue)
            {
                if (quizDto.DurationMinutes.Value < 1)
                {
                    return BadRequest(new { message = "Duration must be at least 1 minute" });
                }
                existingQuiz.DurationMinutes = quizDto.DurationMinutes.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuizExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Quiz updated successfully" });
        }

        // DELETE: api/quizzes/5
        [HttpDelete("quizzes/{id}")]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            if (_context.Quizzes == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
            {
                return NotFound(new { message = "Quiz not found" });
            }

            var hasQuestions = await _context.Questions.AnyAsync(q => q.QuizId == id);
            if (hasQuestions)
            {
                return BadRequest(new { message = "Cannot delete quiz. There are questions associated with this quiz. Please delete all questions first." });
            }

            var hasScores = await _context.Scores.AnyAsync(s => s.QuizId == id);
            if (hasScores)
            {
                return BadRequest(new { message = "Cannot delete quiz. There are student scores associated with this quiz." });
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quiz deleted successfully" });
        }

        // DELETE: api/quizzes/5/force
        [HttpDelete("quizzes/{id}/force")]
        public async Task<IActionResult> DeleteQuizForce(int id)
        {
            if (_context.Quizzes == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
            {
                return NotFound(new { message = "Quiz not found" });
            }

            var hasScores = await _context.Scores.AnyAsync(s => s.QuizId == id);
            if (hasScores)
            {
                return BadRequest(new { message = "Cannot delete quiz. There are student scores associated with this quiz." });
            }

            var questions = await _context.Questions
                .Where(q => q.QuizId == id)
                .ToListAsync();

            foreach (var question in questions)
            {
                var answers = await _context.Answers
                    .Where(a => a.QuestionId == question.QuestionId)
                    .ToListAsync();

                if (answers.Any())
                {
                    _context.Answers.RemoveRange(answers);
                }
            }

            if (questions.Any())
            {
                _context.Questions.RemoveRange(questions);
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quiz and all related questions and answers deleted successfully" });
        }

        private bool QuizExists(int id)
        {
            return (_context.Quizzes?.Any(e => e.QuizId == id)).GetValueOrDefault();
        }
    }

    // DTOs
    public class QuizCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title max 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes")]
        public int DurationMinutes { get; set; }
    }

    public class QuizUpdateDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes")]
        public int? DurationMinutes { get; set; }
    }

    public class QuizResponseDto
    {
        public int QuizId { get; set; }
        public int ModuleId { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int QuestionCount { get; set; }
    }

    public class QuizDetailDto
    {
        public int QuizId { get; set; }
        public int ModuleId { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ModuleName { get; set; }
        public int CourseId { get; set; }
        public List<QuestionWithAnswersDto> Questions { get; set; } = new List<QuestionWithAnswersDto>();
    }

    public class QuestionWithAnswersDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public List<AnswerDto> Answers { get; set; } = new List<AnswerDto>();
    }

    public class AnswerDto
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
    }
}