using E_Learning_Quiz.Data;
using E_Learning_Quiz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly DBText _context;

        public CoursesController(DBText context)
        {
            _context = context;
        }

        // GET: api/Courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseResponseDto>>> GetCourses([FromQuery] string? search)
        {
            if (_context.Courses == null)
            {
                return NotFound();
            }

            var query = _context.Courses.Where(c => c.IsActive == true);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    c.Title.Contains(search) ||
                    c.Description.Contains(search));
            }

            var courses = await query
                .Select(c => new CourseResponseDto
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorId = c.InstructorId,
                    CreatedAt = c.CreatedAt,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET: api/Courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDetailDto>> GetCourse(int id)
        {
            if (_context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Where(c => c.CourseId == id)
                .Select(c => new CourseDetailDto
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorId = c.InstructorId,
                    CreatedAt = c.CreatedAt,
                    IsActive = c.IsActive,
                    Modules = _context.Modules
                        .Where(m => m.CourseId == c.CourseId)
                        .Select(m => new ModuleDto
                        {
                            ModuleId = m.ModuleId,
                            Title = m.Title,
                            OrderIndex = m.OrderIndex,
                            Quizzes = _context.Quizzes
                                .Where(q => q.ModuleId == m.ModuleId)
                                .Select(q => new QuizDto
                                {
                                    QuizId = q.QuizId,
                                    Title = q.Title
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            return Ok(course);
        }

        // POST: api/Courses
        [HttpPost]
        public async Task<ActionResult<CourseResponseDto>> PostCourse(CourseCreateDto courseDto)
        {
            if (_context.Courses == null)
            {
                return Problem("Entity set 'DBText.Courses' is null.");
            }

            var instructorExists = await _context.Users
                .AnyAsync(u => u.UserId == courseDto.InstructorId && u.Role == "Instructor");

            if (!instructorExists)
            {
                return BadRequest(new { message = "Invalid instructor ID or user is not an instructor" });
            }

            var course = new Courses
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                InstructorId = courseDto.InstructorId,
                CreatedAt = DateTime.Now,
                IsActive = true 
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var response = new CourseResponseDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                CreatedAt = course.CreatedAt,
                IsActive = course.IsActive
            };

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, response);
        }

        // PUT: api/Courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(int id, CourseUpdateDto courseDto)
        {
            var existingCourse = await _context.Courses.FindAsync(id);

            if (existingCourse == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            if (!string.IsNullOrEmpty(courseDto.Title))
            {
                existingCourse.Title = courseDto.Title;
            }

            if (!string.IsNullOrEmpty(courseDto.Description))
            {
                existingCourse.Description = courseDto.Description;
            }

            if (courseDto.InstructorId.HasValue)
            {
                // Validasi instructor
                var instructorExists = await _context.Users
                    .AnyAsync(u => u.UserId == courseDto.InstructorId.Value && u.Role == "Instructor");

                if (!instructorExists)
                {
                    return BadRequest(new { message = "Invalid instructor ID" });
                }

                existingCourse.InstructorId = courseDto.InstructorId.Value;
            }

            if (courseDto.IsActive.HasValue)
            {
                existingCourse.IsActive = courseDto.IsActive.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Course updated successfully" });
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            if (_context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            course.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course deleted successfully" });
        }

        // DELETE: api/Courses/5/permanent
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> DeleteCoursePermanent(int id)
        {
            if (_context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course permanently deleted" });
        }

        private bool CourseExists(int id)
        {
            return (_context.Courses?.Any(e => e.CourseId == id)).GetValueOrDefault();
        }
    }

    public class CourseCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title max 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description max 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Instructor ID is required")]
        public int InstructorId { get; set; }
    }

    public class CourseUpdateDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? InstructorId { get; set; }

        public bool? IsActive { get; set; } 
    }

    public class CourseResponseDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int InstructorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } 
    }

    public class CourseDetailDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int InstructorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<ModuleDto> Modules { get; set; } = new List<ModuleDto>();
    }

    public class ModuleDto
    {
        public int ModuleId { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public List<QuizDto> Quizzes { get; set; } = new List<QuizDto>();
    }

    public class QuizDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; }
    }
}