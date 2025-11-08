using E_Learning_Quiz.Data;
using E_Learning_Quiz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Controllers
{
    [Route("api")]
    [ApiController]
    public class AnswersController : ControllerBase
    {
        private readonly DBText _context;

        public AnswersController(DBText context)
        {
            _context = context;
        }

        // GET: api/questions/{questionId}/answers
        // Ambil semua jawaban dari pertanyaan
        [HttpGet("questions/{questionId}/answers")]
        public async Task<ActionResult<IEnumerable<AnswerResponseDto>>> GetAnswersByQuestion(int questionId)
        {
            // Validasi: Cek apakah question ada
            var questionExists = await _context.Questions.AnyAsync(q => q.QuestionId == questionId);
            if (!questionExists)
            {
                return NotFound(new { message = "Question not found" });
            }

            var answers = await _context.Answers
                .Where(a => a.QuestionId == questionId)
                .Select(a => new AnswerResponseDto
                {
                    AnswerId = a.AnswerId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                })
                .ToListAsync();

            return Ok(answers);
        }

        // POST: api/questions/{questionId}/answers
        // Tambah jawaban baru
        [HttpPost("questions/{questionId}/answers")]
        public async Task<ActionResult<AnswerResponseDto>> PostAnswer(int questionId, AnswerCreateDto answerDto)
        {
            if (_context.Answers == null)
            {
                return Problem("Entity set 'DBText.Answers' is null.");
            }

            // Validasi: Cek apakah question ada
            var questionExists = await _context.Questions.AnyAsync(q => q.QuestionId == questionId);
            if (!questionExists)
            {
                return NotFound(new { message = "Question not found" });
            }

            // Validasi: Jika ini jawaban benar, cek apakah sudah ada jawaban benar lainnya
            if (answerDto.IsCorrect)
            {
                var hasCorrectAnswer = await _context.Answers
                    .AnyAsync(a => a.QuestionId == questionId && a.IsCorrect == true);

                if (hasCorrectAnswer)
                {
                    return BadRequest(new { message = "This question already has a correct answer. Please update the existing correct answer or set it to false first." });
                }
            }

            // Buat answer baru
            var answer = new Answers
            {
                QuestionId = questionId,
                AnswerText = answerDto.AnswerText,
                IsCorrect = answerDto.IsCorrect
            };

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            var response = new AnswerResponseDto
            {
                AnswerId = answer.AnswerId,
                QuestionId = answer.QuestionId,
                AnswerText = answer.AnswerText,
                IsCorrect = answer.IsCorrect
            };

            return CreatedAtAction(nameof(GetAnswersByQuestion), new { questionId = answer.QuestionId }, response);
        }

        // PUT: api/answers/5
        // Update jawaban
        [HttpPut("answers/{id}")]
        public async Task<IActionResult> PutAnswer(int id, AnswerUpdateDto answerDto)
        {
            var existingAnswer = await _context.Answers.FindAsync(id);

            if (existingAnswer == null)
            {
                return NotFound(new { message = "Answer not found" });
            }

            // Update AnswerText
            if (!string.IsNullOrEmpty(answerDto.AnswerText))
            {
                existingAnswer.AnswerText = answerDto.AnswerText;
            }

            // Update IsCorrect dengan validasi
            if (answerDto.IsCorrect.HasValue)
            {
                // Jika mengubah menjadi benar, cek apakah sudah ada jawaban benar lainnya
                if (answerDto.IsCorrect.Value == true)
                {
                    var hasOtherCorrectAnswer = await _context.Answers
                        .AnyAsync(a => a.QuestionId == existingAnswer.QuestionId &&
                                      a.IsCorrect == true &&
                                      a.AnswerId != id);

                    if (hasOtherCorrectAnswer)
                    {
                        return BadRequest(new { message = "This question already has another correct answer. Please set it to false first." });
                    }
                }

                existingAnswer.IsCorrect = answerDto.IsCorrect.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AnswerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Answer updated successfully" });
        }

        // DELETE: api/answers/5
        // Hapus jawaban
        [HttpDelete("answers/{id}")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            if (_context.Answers == null)
            {
                return NotFound();
            }

            var answer = await _context.Answers.FindAsync(id);
            if (answer == null)
            {
                return NotFound(new { message = "Answer not found" });
            }

            // Warning: Jika ini adalah jawaban benar terakhir
            if (answer.IsCorrect)
            {
                var correctAnswersCount = await _context.Answers
                    .CountAsync(a => a.QuestionId == answer.QuestionId && a.IsCorrect == true);

                if (correctAnswersCount == 1)
                {
                    // Beri warning tapi tetap izinkan delete
                    _context.Answers.Remove(answer);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Answer deleted successfully",
                        warning = "This was the only correct answer for this question. The question now has no correct answer."
                    });
                }
            }

            _context.Answers.Remove(answer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Answer deleted successfully" });
        }

        // GET: api/answers/5
        // Ambil detail jawaban (bonus endpoint)
        [HttpGet("answers/{id}")]
        public async Task<ActionResult<AnswerDetailDto>> GetAnswer(int id)
        {
            if (_context.Answers == null)
            {
                return NotFound();
            }

            var answer = await _context.Answers
                .Where(a => a.AnswerId == id)
                .Select(a => new AnswerDetailDto
                {
                    AnswerId = a.AnswerId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect,
                    Question = new QuestionBasicDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = _context.Questions
                            .Where(q => q.QuestionId == a.QuestionId)
                            .Select(q => q.QuestionText)
                            .FirstOrDefault()
                    }
                })
                .FirstOrDefaultAsync();

            if (answer == null)
            {
                return NotFound(new { message = "Answer not found" });
            }

            return Ok(answer);
        }

        // GET: api/questions/{questionId}/answers/correct
        // Ambil hanya jawaban yang benar (bonus endpoint)
        [HttpGet("questions/{questionId}/answers/correct")]
        public async Task<ActionResult<AnswerResponseDto>> GetCorrectAnswer(int questionId)
        {
            var questionExists = await _context.Questions.AnyAsync(q => q.QuestionId == questionId);
            if (!questionExists)
            {
                return NotFound(new { message = "Question not found" });
            }

            var correctAnswer = await _context.Answers
                .Where(a => a.QuestionId == questionId && a.IsCorrect == true)
                .Select(a => new AnswerResponseDto
                {
                    AnswerId = a.AnswerId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                })
                .FirstOrDefaultAsync();

            if (correctAnswer == null)
            {
                return NotFound(new { message = "No correct answer found for this question" });
            }

            return Ok(correctAnswer);
        }

        private bool AnswerExists(int id)
        {
            return (_context.Answers?.Any(e => e.AnswerId == id)).GetValueOrDefault();
        }
    }

    // DTOs
    public class AnswerCreateDto
    {
        [Required(ErrorMessage = "Answer text is required")]
        [StringLength(500, ErrorMessage = "Answer text max 500 characters")]
        public string AnswerText { get; set; }

        [Required(ErrorMessage = "IsCorrect field is required")]
        public bool IsCorrect { get; set; }
    }

    public class AnswerUpdateDto
    {
        [StringLength(500, ErrorMessage = "Answer text max 500 characters")]
        public string? AnswerText { get; set; }

        public bool? IsCorrect { get; set; }
    }

    public class AnswerResponseDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class AnswerDetailDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public QuestionBasicDto Question { get; set; }
    }

    public class QuestionBasicDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
    }
}