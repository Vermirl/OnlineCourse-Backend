using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OnlineCourse.Data;
using OnlineCourse.DTOs;
using OnlineCourse.Models;

namespace OnlineCourse.Controllers
{
    // ==================== QUIZ CONTROLLER ====================
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public QuizzesController(AppDbContext db) => _db = db;

        // UC-05 (Student): Lấy quiz theo bài học
        [HttpGet("lesson/{lessonId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetByLesson(int lessonId)
        {
            var quizzes = await _db.Quizzes
                .Where(q => q.LessonId == lessonId)
                .Select(q => new QuizDto
                {
                    Id = q.Id, Question = q.Question,
                    OptionA = q.OptionA, OptionB = q.OptionB,
                    OptionC = q.OptionC, OptionD = q.OptionD,
                    LessonId = q.LessonId
                }).ToListAsync();
            return Ok(quizzes);
        }

        // UC-05 (Student): Nộp quiz
        [HttpPost("submit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(SubmitQuizDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var quiz = await _db.Quizzes.FindAsync(dto.QuizId);
            if (quiz == null) return NotFound();

            bool isCorrect = quiz.CorrectAnswer.Equals(dto.SelectedAnswer, StringComparison.OrdinalIgnoreCase);

            _db.QuizResults.Add(new QuizResult
            {
                StudentId = userId, QuizId = dto.QuizId,
                SelectedAnswer = dto.SelectedAnswer, IsCorrect = isCorrect
            });
            await _db.SaveChangesAsync();

            return Ok(new QuizResultDto
            {
                IsCorrect = isCorrect,
                CorrectAnswer = quiz.CorrectAnswer,
                Explanation = quiz.Explanation
            });
        }

        // UC-07 (Instructor): Lấy quiz theo lesson (full detail)
        [HttpGet("lesson/{lessonId}/detail")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetByLessonDetail(int lessonId)
        {
            var quizzes = await _db.Quizzes
                .Where(q => q.LessonId == lessonId)
                .Select(q => new QuizDetailDto
                {
                    Id = q.Id, Question = q.Question,
                    OptionA = q.OptionA, OptionB = q.OptionB,
                    OptionC = q.OptionC, OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer, Explanation = q.Explanation,
                    LessonId = q.LessonId
                }).ToListAsync();
            return Ok(quizzes);
        }

        // UC-07 (Instructor): Tạo quiz
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Create(CreateQuizDto dto)
        {
            var quiz = new Quiz
            {
                Question = dto.Question, OptionA = dto.OptionA, OptionB = dto.OptionB,
                OptionC = dto.OptionC, OptionD = dto.OptionD,
                CorrectAnswer = dto.CorrectAnswer, Explanation = dto.Explanation,
                LessonId = dto.LessonId
            };
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Tạo câu hỏi thành công!", quizId = quiz.Id });
        }

        // UC-07 (Instructor): Xóa quiz
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _db.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound();
            _db.Quizzes.Remove(quiz);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã xóa câu hỏi." });
        }
    }

    // ==================== ENROLLMENT CONTROLLER ====================
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EnrollmentsController(AppDbContext db) => _db = db;

        // UC-03: Mua khóa học (mock payment)
        [HttpPost("enroll/{courseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var course = await _db.Courses.FindAsync(courseId);
            if (course == null || course.Status != "Published")
                return BadRequest(new { message = "Khóa học không tồn tại hoặc chưa xuất bản." });

            var exists = await _db.Enrollments.AnyAsync(e => e.StudentId == userId && e.CourseId == courseId);
            if (exists) return BadRequest(new { message = "Bạn đã đăng ký khóa học này rồi." });

            _db.Enrollments.Add(new Enrollment { StudentId = userId, CourseId = courseId });
            await _db.SaveChangesAsync();

            return Ok(new { message = "🎉 Đăng ký thành công! (Mock Payment)" });
        }

        // UC-04: Xem khóa học đã mua
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyEnrollments()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var enrollments = await _db.Enrollments
                .Include(e => e.Course).ThenInclude(c => c.Lessons)
                .Where(e => e.StudentId == userId)
                .ToListAsync();

            var result = new List<EnrollmentDto>();
            foreach (var e in enrollments)
            {
                int total = e.Course.Lessons.Count;
                int completed = await _db.LessonProgresses
                    .CountAsync(p => p.EnrollmentId == e.Id && p.IsCompleted);
                int percent = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100);
                result.Add(new EnrollmentDto
                {
                    Id = e.Id, CourseId = e.CourseId, CourseTitle = e.Course.Title,
                    ThumbnailUrl = e.Course.ThumbnailUrl, ProgressPercent = percent,
                    IsCompleted = e.IsCompleted, EnrolledAt = e.EnrolledAt
                });
            }
            return Ok(result);
        }

        // UC-06: Kiểm tra đã mua chưa
        [HttpGet("check/{courseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CheckEnrolled(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var enrolled = await _db.Enrollments.AnyAsync(e => e.StudentId == userId && e.CourseId == courseId);
            return Ok(new { enrolled });
        }

        // UC-06: Lấy chứng chỉ (khi hoàn thành 100%)
        [HttpGet("certificate/{enrollmentId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetCertificate(int enrollmentId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var enrollment = await _db.Enrollments
                .Include(e => e.Course).ThenInclude(c => c.Instructor)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);

            if (enrollment == null) return NotFound();
            if (!enrollment.IsCompleted) return BadRequest(new { message = "Bạn chưa hoàn thành khóa học này." });

            return Ok(new
            {
                studentName = enrollment.Student.FullName,
                courseTitle = enrollment.Course.Title,
                instructorName = enrollment.Course.Instructor.FullName,
                completedAt = enrollment.CompletedAt,
                enrollmentId = enrollment.Id
            });
        }
    }

    // ==================== ADMIN CONTROLLER ====================
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) => _db = db;

        // UC-09: Lấy danh sách user
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            var users = await query.Select(u => new UserAdminDto
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email,
                Role = u.Role, IsActive = u.IsActive, CreatedAt = u.CreatedAt
            }).ToListAsync();
            return Ok(users);
        }

        // UC-09: Khóa/Mở tài khoản
        [HttpPost("users/{id}/toggle")]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Role == "Admin") return BadRequest(new { message = "Không thể khóa tài khoản Admin." });
            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { message = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.", isActive = user.IsActive });
        }

        // UC-09: Đổi vai trò
        [HttpPost("users/{id}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] string role)
        {
            if (role != "Student" && role != "Instructor")
                return BadRequest(new { message = "Vai trò không hợp lệ." });
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Role = role;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã đổi vai trò." });
        }

        // Thống kê tổng quan
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            return Ok(new StatsDto
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalCourses = await _db.Courses.CountAsync(c => c.Status == "Published"),
                TotalEnrollments = await _db.Enrollments.CountAsync(),
                PendingCourses = await _db.Courses.CountAsync(c => c.Status == "Pending"),
                TotalRevenue = await _db.Enrollments
                    .Include(e => e.Course)
                    .SumAsync(e => e.Course.Price)
            });
        }
    }

    // ==================== CATEGORIES CONTROLLER ====================
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cats = await _db.Categories.Select(c => new { c.Id, c.Name }).ToListAsync();
            return Ok(cats);
        }
    }
}
