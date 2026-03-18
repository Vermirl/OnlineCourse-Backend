using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineCourse.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required] public string FullName { get; set; } = "";
        [Required] public string Email { get; set; } = "";
        [Required] public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "Student"; // Student | Instructor | Admin
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }

    public class Category
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = "";
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class Course
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; } = "";
        public string Status { get; set; } = "Draft"; // Draft | Pending | Published | Rejected
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int InstructorId { get; set; }
        public User Instructor { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }

    public class Lesson
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string VideoUrl { get; set; } = "";
        public int OrderIndex { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();
    }

    public class Quiz
    {
        public int Id { get; set; }
        [Required] public string Question { get; set; } = "";
        public string OptionA { get; set; } = "";
        public string OptionB { get; set; } = "";
        public string OptionC { get; set; } = "";
        public string OptionD { get; set; } = "";
        public string CorrectAnswer { get; set; } = "A"; // A|B|C|D
        public string Explanation { get; set; } = "";
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        public ICollection<QuizResult> Results { get; set; } = new List<QuizResult>();
    }

    public class Enrollment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }

    public class LessonProgress
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public Enrollment Enrollment { get; set; } = null!;
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
    }

    public class QuizResult
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; } = null!;
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;
        public string SelectedAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }
        public DateTime AttemptAt { get; set; } = DateTime.UtcNow;
    }
}
