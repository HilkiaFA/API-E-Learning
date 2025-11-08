using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Models
{
    public class Questions
    {
        [Key]
        public int QuestionId { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; }
    }
}
