using E_Learning_Quiz.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace E_Learning_Quiz.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly DBText _context;

        public DashboardController(DBText context)
        {
            _context = context;
        }

        // GET: api/dashboard/overview
        // Statistik jumlah user, kursus, kuis, skor rata-rata
        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetOverview()
        {
            try
            {
                // Total Users by Role
                var totalUsers = await _context.Users.CountAsync();
                var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
                var totalInstructors = await _context.Users.CountAsync(u => u.Role == "Instructor");
                var totalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin");

                // Total Courses
                var totalCourses = await _context.Courses.CountAsync();
                var activeCourses = await _context.Courses.CountAsync(c => c.IsActive == true);
                var inactiveCourses = await _context.Courses.CountAsync(c => c.IsActive == false);

                // Total Modules
                var totalModules = await _context.Modules.CountAsync();

                // Total Quizzes
                var totalQuizzes = await _context.Quizzes.CountAsync();

                // Total Questions
                var totalQuestions = await _context.Questions.CountAsync();

                // Total Scores/Attempts
                var totalAttempts = await _context.Scores.CountAsync();

                // Average Score
                var averageScore = await _context.Scores.AnyAsync()
                    ? await _context.Scores.AverageAsync(s => s.ScoreValue)
                    : 0;

                // Highest Score
                var highestScore = await _context.Scores.AnyAsync()
                    ? await _context.Scores.MaxAsync(s => s.ScoreValue)
                    : 0;

                // Lowest Score
                var lowestScore = await _context.Scores.AnyAsync()
                    ? await _context.Scores.MinAsync(s => s.ScoreValue)
                    : 0;

                // Recent Activities (Last 5 scores)
                var recentActivities = await _context.Scores
                    .OrderByDescending(s => s.TakenAt)
                    .Take(5)
                    .Select(s => new RecentActivityDto
                    {
                        StudentName = _context.Users
                            .Where(u => u.UserId == s.UserId)
                            .Select(u => u.FullName)
                            .FirstOrDefault(),
                        QuizTitle = _context.Quizzes
                            .Where(q => q.QuizId == s.QuizId)
                            .Select(q => q.Title)
                            .FirstOrDefault(),
                        Score = s.ScoreValue,
                        TakenAt = s.TakenAt
                    })
                    .ToListAsync();

                // Top 5 Students
                var topStudents = await _context.Scores
                    .GroupBy(s => s.UserId)
                    .Select(g => new TopStudentDto
                    {
                        UserId = g.Key,
                        StudentName = _context.Users
                            .Where(u => u.UserId == g.Key)
                            .Select(u => u.FullName)
                            .FirstOrDefault(),
                        AverageScore = g.Average(s => s.ScoreValue),
                        TotalQuizzesTaken = g.Count()
                    })
                    .OrderByDescending(s => s.AverageScore)
                    .Take(5)
                    .ToListAsync();

                // Courses with most enrollments
                var popularCourses = await _context.Scores
                    .Join(_context.Quizzes, s => s.QuizId, q => q.QuizId, (s, q) => new { s, q })
                    .Join(_context.Modules, sq => sq.q.ModuleId, m => m.ModuleId, (sq, m) => new { sq.s, m })
                    .GroupBy(x => x.m.CourseId)
                    .Select(g => new PopularCourseDto
                    {
                        CourseId = g.Key,
                        CourseTitle = _context.Courses
                            .Where(c => c.CourseId == g.Key)
                            .Select(c => c.Title)
                            .FirstOrDefault(),
                        TotalEnrollments = g.Select(x => x.s.UserId).Distinct().Count(),
                        TotalAttempts = g.Count()
                    })
                    .OrderByDescending(c => c.TotalEnrollments)
                    .Take(5)
                    .ToListAsync();

                var overview = new DashboardOverviewDto
                {
                    // Users Statistics
                    TotalUsers = totalUsers,
                    TotalStudents = totalStudents,
                    TotalInstructors = totalInstructors,
                    TotalAdmins = totalAdmins,

                    // Content Statistics
                    TotalCourses = totalCourses,
                    ActiveCourses = activeCourses,
                    InactiveCourses = inactiveCourses,
                    TotalModules = totalModules,
                    TotalQuizzes = totalQuizzes,
                    TotalQuestions = totalQuestions,

                    // Score Statistics
                    TotalAttempts = totalAttempts,
                    AverageScore = Math.Round(averageScore, 2),
                    HighestScore = Math.Round(highestScore, 2),
                    LowestScore = Math.Round(lowestScore, 2),

                    // Activity Data
                    RecentActivities = recentActivities,
                    TopStudents = topStudents,
                    PopularCourses = popularCourses
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving dashboard overview", error = ex.Message });
            }
        }

        // GET: api/dashboard/course-stats
        // Data agregat kursus menggunakan stored procedure
        [HttpGet("course-stats")]
        public async Task<ActionResult<IEnumerable<CourseStatsDto>>> GetCourseStats()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var courseStats = new List<CourseStatsDto>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "sp_GetCourseOverview";
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stats = new CourseStatsDto
                            {
                                CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
                                CourseTitle = reader.GetString(reader.GetOrdinal("CourseTitle")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Description")),
                                InstructorName = reader.IsDBNull(reader.GetOrdinal("InstructorName"))
                                    ? "N/A"
                                    : reader.GetString(reader.GetOrdinal("InstructorName")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                TotalModules = reader.GetInt32(reader.GetOrdinal("TotalModules")),
                                TotalQuizzes = reader.GetInt32(reader.GetOrdinal("TotalQuizzes")),
                                TotalStudentsEnrolled = reader.GetInt32(reader.GetOrdinal("TotalStudentsEnrolled")),
                                AverageScore = reader.IsDBNull(reader.GetOrdinal("AverageScore"))
                                    ? 0
                                    : Math.Round(reader.GetDecimal(reader.GetOrdinal("AverageScore")), 2),
                                TotalAttempts = reader.GetInt32(reader.GetOrdinal("TotalAttempts"))
                            };
                            courseStats.Add(stats);
                        }
                    }
                }

                await connection.CloseAsync();
                return Ok(courseStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving course statistics", error = ex.Message });
            }
        }

        // GET: api/dashboard/stats/daily
        // Bonus: Daily activity statistics
        [HttpGet("stats/daily")]
        public async Task<ActionResult<IEnumerable<DailyStatsDto>>> GetDailyStats([FromQuery] int days = 7)
        {
            try
            {
                var startDate = DateTime.Now.Date.AddDays(-days);

                var dailyStats = await _context.Scores
                    .Where(s => s.TakenAt >= startDate)
                    .GroupBy(s => s.TakenAt.Date)
                    .Select(g => new DailyStatsDto
                    {
                        Date = g.Key,
                        TotalAttempts = g.Count(),
                        UniqueStudents = g.Select(s => s.UserId).Distinct().Count(),
                        AverageScore = Math.Round(g.Average(s => s.ScoreValue), 2)
                    })
                    .OrderBy(d => d.Date)
                    .ToListAsync();

                return Ok(dailyStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving daily statistics", error = ex.Message });
            }
        }

        // GET: api/dashboard/stats/quiz-performance
        // Bonus: Quiz performance statistics
        [HttpGet("stats/quiz-performance")]
        public async Task<ActionResult<IEnumerable<QuizPerformanceDto>>> GetQuizPerformance()
        {
            try
            {
                var quizPerformance = await _context.Scores
                    .GroupBy(s => s.QuizId)
                    .Select(g => new QuizPerformanceDto
                    {
                        QuizId = g.Key,
                        QuizTitle = _context.Quizzes
                            .Where(q => q.QuizId == g.Key)
                            .Select(q => q.Title)
                            .FirstOrDefault(),
                        TotalAttempts = g.Count(),
                        AverageScore = Math.Round(g.Average(s => s.ScoreValue), 2),
                        HighestScore = Math.Round(g.Max(s => s.ScoreValue), 2),
                        LowestScore = Math.Round(g.Min(s => s.ScoreValue), 2),
                        PassRate = Math.Round((decimal)g.Count(s => s.ScoreValue >= 60) / g.Count() * 100, 2)
                    })
                    .OrderByDescending(q => q.TotalAttempts)
                    .ToListAsync();

                return Ok(quizPerformance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving quiz performance", error = ex.Message });
            }
        }
    }

    // DTOs
    public class DashboardOverviewDto
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalAdmins { get; set; }

        // Content Statistics
        public int TotalCourses { get; set; }
        public int ActiveCourses { get; set; }
        public int InactiveCourses { get; set; }
        public int TotalModules { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalQuestions { get; set; }

        // Score Statistics
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }

        // Activity Data
        public List<RecentActivityDto> RecentActivities { get; set; }
        public List<TopStudentDto> TopStudents { get; set; }
        public List<PopularCourseDto> PopularCourses { get; set; }
    }

    public class RecentActivityDto
    {
        public string StudentName { get; set; }
        public string QuizTitle { get; set; }
        public decimal Score { get; set; }
        public DateTime TakenAt { get; set; }
    }

    public class TopStudentDto
    {
        public int UserId { get; set; }
        public string StudentName { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalQuizzesTaken { get; set; }
    }

    public class PopularCourseDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalAttempts { get; set; }
    }

    public class CourseStatsDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string Description { get; set; }
        public string InstructorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int TotalModules { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalStudentsEnrolled { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalAttempts { get; set; }
    }

    public class DailyStatsDto
    {
        public DateTime Date { get; set; }
        public int TotalAttempts { get; set; }
        public int UniqueStudents { get; set; }
        public decimal AverageScore { get; set; }
    }

    public class QuizPerformanceDto
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public decimal PassRate { get; set; }
    }
}