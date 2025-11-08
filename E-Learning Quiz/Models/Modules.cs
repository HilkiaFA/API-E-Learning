using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Models
{
    public class Modules
    {
        [Key]
        public int ModuleId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
