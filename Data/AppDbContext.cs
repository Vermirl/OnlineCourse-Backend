using Microsoft.EntityFrameworkCore;
using OnlineCourse.Models;

namespace OnlineCourse.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
        public DbSet<QuizResult> QuizResults => Set<QuizResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(u => u.Courses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizResult>()
                .HasOne(q => q.Student)
                .WithMany()
                .HasForeignKey(q => q.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LessonProgress>()
                .HasOne(lp => lp.Lesson)
                .WithMany(l => l.Progresses)
                .HasForeignKey(lp => lp.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LessonProgress>()
                .HasOne(lp => lp.Enrollment)
                .WithMany(e => e.LessonProgresses)
                .HasForeignKey(lp => lp.EnrollmentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Lập trình Web" },
                new Category { Id = 2, Name = "Lập trình Mobile" },
                new Category { Id = 3, Name = "Database" },
                new Category { Id = 4, Name = "DevOps" }
            );

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                FullName = "Administrator",
                Email = "admin@course.com",
                PasswordHash = "$2a$11$rBnqQkVZk5L8eZ1n2kNv8.9j5n3mX4vQ7wYpL6tH2oR1sD0cE3fGu",
                Role = "Admin",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1)
            });
        }
    }
}