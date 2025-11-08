using E_Learning_Quiz.Data;
using E_Learning_Quiz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Controllers
{
    [Route("api")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly DBText _context;

        public ModulesController(DBText context)
        {
            _context = context;
        }

        // GET: api/courses/{courseId}/modules
        // Ambil semua modul dari kursus
        [HttpGet("courses/{courseId}/modules")]
        public async Task<ActionResult<IEnumerable<ModuleResponseDto>>> GetModulesByCourse(int courseId)
        {
            // Validasi: Cek apakah course ada
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == courseId);
            if (!courseExists)
            {
                return NotFound(new { message = "Course not found" });
            }

            var modules = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ModuleResponseDto
                {
                    ModuleId = m.ModuleId,
                    CourseId = m.CourseId,
                    Title = m.Title,
                    Content = m.Content,
                    OrderIndex = m.OrderIndex,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(modules);
        }

        // GET: api/modules/5
        // Ambil detail modul
        [HttpGet("modules/{id}")]
        public async Task<ActionResult<ModuleDetailDto>> GetModule(int id)
        {
            if (_context.Modules == null)
            {
                return NotFound();
            }

            var module = await _context.Modules
                .Where(m => m.ModuleId == id)
                .Select(m => new ModuleDetailDto
                {
                    ModuleId = m.ModuleId,
                    CourseId = m.CourseId,
                    Title = m.Title,
                    Content = m.Content,
                    OrderIndex = m.OrderIndex,
                    CreatedAt = m.CreatedAt,
                    Course = new CourseBasicDto
                    {
                        CourseId = m.CourseId,
                        Title = _context.Courses
                            .Where(c => c.CourseId == m.CourseId)
                            .Select(c => c.Title)
                            .FirstOrDefault()
                    },
                    Quizzes = _context.Quizzes
                        .Where(q => q.ModuleId == m.ModuleId)
                        .Select(q => new QuizBasicDto
                        {
                            QuizId = q.QuizId,
                            Title = q.Title
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (module == null)
            {
                return NotFound(new { message = "Module not found" });
            }

            return Ok(module);
        }

        // POST: api/courses/{courseId}/modules
        // Tambah modul ke kursus
        [HttpPost("courses/{courseId}/modules")]
        public async Task<ActionResult<ModuleResponseDto>> PostModule(int courseId, ModuleCreateDto moduleDto)
        {
            if (_context.Modules == null)
            {
                return Problem("Entity set 'DBText.Modules' is null.");
            }

            // Validasi: Cek apakah course ada
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == courseId);
            if (!courseExists)
            {
                return NotFound(new { message = "Course not found" });
            }

            // Validasi: Cek apakah OrderIndex sudah digunakan
            var orderExists = await _context.Modules
                .AnyAsync(m => m.CourseId == courseId && m.OrderIndex == moduleDto.OrderIndex);

            if (orderExists)
            {
                return BadRequest(new { message = "OrderIndex already exists in this course" });
            }

            // Buat modul baru
            var module = new Modules
            {
                CourseId = courseId,
                Title = moduleDto.Title,
                Content = moduleDto.Content,
                OrderIndex = moduleDto.OrderIndex,
                CreatedAt = DateTime.Now
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            var response = new ModuleResponseDto
            {
                ModuleId = module.ModuleId,
                CourseId = module.CourseId,
                Title = module.Title,
                Content = module.Content,
                OrderIndex = module.OrderIndex,
                CreatedAt = module.CreatedAt
            };

            return CreatedAtAction(nameof(GetModule), new { id = module.ModuleId }, response);
        }

        // PUT: api/modules/5
        // Edit modul
        [HttpPut("modules/{id}")]
        public async Task<IActionResult> PutModule(int id, ModuleUpdateDto moduleDto)
        {
            var existingModule = await _context.Modules.FindAsync(id);

            if (existingModule == null)
            {
                return NotFound(new { message = "Module not found" });
            }

            // Update hanya field yang tidak null/empty
            if (!string.IsNullOrEmpty(moduleDto.Title))
            {
                existingModule.Title = moduleDto.Title;
            }

            if (!string.IsNullOrEmpty(moduleDto.Content))
            {
                existingModule.Content = moduleDto.Content;
            }

            if (moduleDto.OrderIndex.HasValue)
            {
                // Validasi: Cek apakah OrderIndex sudah digunakan oleh modul lain di course yang sama
                var orderExists = await _context.Modules
                    .AnyAsync(m => m.CourseId == existingModule.CourseId &&
                                   m.OrderIndex == moduleDto.OrderIndex.Value &&
                                   m.ModuleId != id);

                if (orderExists)
                {
                    return BadRequest(new { message = "OrderIndex already exists in this course" });
                }

                existingModule.OrderIndex = moduleDto.OrderIndex.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ModuleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Module updated successfully" });
        }

        // DELETE: api/modules/5
        // Hapus modul
        [HttpDelete("modules/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            if (_context.Modules == null)
            {
                return NotFound();
            }

            var module = await _context.Modules.FindAsync(id);
            if (module == null)
            {
                return NotFound(new { message = "Module not found" });
            }

            // Cek apakah ada quiz yang terkait dengan modul ini
            var hasQuizzes = await _context.Quizzes.AnyAsync(q => q.ModuleId == id);
            if (hasQuizzes)
            {
                return BadRequest(new { message = "Cannot delete module. There are quizzes associated with this module." });
            }

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Module deleted successfully" });
        }

        // DELETE: api/modules/5/force
        // Hapus modul beserta quiz (force delete)
        [HttpDelete("modules/{id}/force")]
        public async Task<IActionResult> DeleteModuleForce(int id)
        {
            if (_context.Modules == null)
            {
                return NotFound();
            }

            var module = await _context.Modules.FindAsync(id);
            if (module == null)
            {
                return NotFound(new { message = "Module not found" });
            }

            // Hapus semua quiz yang terkait
            var relatedQuizzes = await _context.Quizzes
                .Where(q => q.ModuleId == id)
                .ToListAsync();

            if (relatedQuizzes.Any())
            {
                _context.Quizzes.RemoveRange(relatedQuizzes);
            }

            // Hapus modul
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Module and all related quizzes deleted successfully" });
        }

        private bool ModuleExists(int id)
        {
            return (_context.Modules?.Any(e => e.ModuleId == id)).GetValueOrDefault();
        }
    }

    // DTOs
    public class ModuleCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title max 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }

        [Required(ErrorMessage = "OrderIndex is required")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderIndex must be greater than 0")]
        public int OrderIndex { get; set; }
    }

    public class ModuleUpdateDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "OrderIndex must be greater than 0")]
        public int? OrderIndex { get; set; }
    }

    public class ModuleResponseDto
    {
        public int ModuleId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ModuleDetailDto
    {
        public int ModuleId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public CourseBasicDto Course { get; set; }
        public List<QuizBasicDto> Quizzes { get; set; } = new List<QuizBasicDto>();
    }

    public class CourseBasicDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
    }

    public class QuizBasicDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; }
    }
}