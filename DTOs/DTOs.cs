namespace OnlineCourse.DTOs
{
    // ===== AUTH =====
    public class RegisterDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Student"; // Student | Instructor
    }

    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = "";
        public string Role { get; set; } = "";
        public string FullName { get; set; } = "";
        public int UserId { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = "";
        public string? AvatarUrl { get; set; }
    }

    // ===== COURSE =====
    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; } = "";
        public string Status { get; set; } = "";
        public string? RejectReason { get; set; }
        public string CategoryName { get; set; } = "";
        public int CategoryId { get; set; }
        public string InstructorName { get; set; } = "";
        public int InstructorId { get; set; }
        public int TotalLessons { get; set; }
        public int TotalEnrollments { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCourseDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; } = "";
        public int CategoryId { get; set; }
    }

    // ===== LESSON =====
    public class LessonDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string VideoUrl { get; set; } = "";
        public int OrderIndex { get; set; }
        public int CourseId { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasQuiz { get; set; }
    }

    public class CreateLessonDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string VideoUrl { get; set; } = "";
        public int OrderIndex { get; set; }
        public int CourseId { get; set; }
    }

    // ===== QUIZ =====
    public class QuizDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = "";
        public string OptionA { get; set; } = "";
        public string OptionB { get; set; } = "";
        public string OptionC { get; set; } = "";
        public string OptionD { get; set; } = "";
        public int LessonId { get; set; }
        // CorrectAnswer & Explanation hidden from student view
    }

    public class QuizDetailDto : QuizDto
    {
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
    }

    public class CreateQuizDto
    {
        public string Question { get; set; } = "";
        public string OptionA { get; set; } = "";
        public string OptionB { get; set; } = "";
        public string OptionC { get; set; } = "";
        public string OptionD { get; set; } = "";
        public string CorrectAnswer { get; set; } = "A";
        public string Explanation { get; set; } = "";
        public int LessonId { get; set; }
    }

    public class SubmitQuizDto
    {
        public int QuizId { get; set; }
        public string SelectedAnswer { get; set; } = "";
    }

    public class QuizResultDto
    {
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
    }

    // ===== ENROLLMENT =====
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public int ProgressPercent { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime EnrolledAt { get; set; }
    }

    // ===== ADMIN =====
    public class UserAdminDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int PendingCourses { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
