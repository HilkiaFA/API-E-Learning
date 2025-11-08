using E_Learning_Quiz.Models;
using Microsoft.EntityFrameworkCore;

namespace E_Learning_Quiz.Data
{
    public class DBText : DbContext
    {
        public DBText(DbContextOptions<DBText> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
          
        }
        public DbSet<Answers> Answers { get; set; }
        public DbSet<Courses> Courses { get; set; }
        public DbSet<Modules> Modules { get; set; }
        public DbSet<Questions> Questions { get; set; }
        public DbSet<Quizzes> Quizzes { get; set; }
        public DbSet<Scores> Scores { get; set; }
        public DbSet<Users> Users { get; set; }
    }
}
