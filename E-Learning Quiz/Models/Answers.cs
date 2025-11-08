using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Models
{
    public class Answers
    {
        [Key]
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }

    }
}
