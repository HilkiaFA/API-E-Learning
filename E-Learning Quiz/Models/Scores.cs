
using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Models
{
    public class Scores
    {
        [Key]
        public int ScoreId { get; set; }
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public decimal ScoreValue { get; set; }
        public DateTime TakenAt { get; set; }
    }
}
