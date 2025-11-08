using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Models
{
    public class Courses
    {
        [Key]
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int InstructorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
