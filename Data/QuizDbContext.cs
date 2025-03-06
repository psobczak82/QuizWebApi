using Microsoft.EntityFrameworkCore;
using QuizWebApi.Models;

namespace QuizWebApi.Data
{
    public class QuizDbContext : DbContext
    {
        public QuizDbContext(DbContextOptions<QuizDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Question>()
                .HasKey(q => q.QuestionId);

            modelBuilder.Entity<Answer>()
                .HasKey(a => a.AnswerId);

            // Relacja 1-n
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId);
        }
    }
}
