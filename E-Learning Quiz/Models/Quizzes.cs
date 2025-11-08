using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Models
{
    public class Quizzes
    {
        [Key]
        public int QuizId { get; set; }
        public int ModuleId { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
