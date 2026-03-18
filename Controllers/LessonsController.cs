using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OnlineCourse.Data;
using OnlineCourse.DTOs;
using OnlineCourse.Models;

namespace OnlineCourse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public LessonsController(AppDbContext db) => _db = db;

        // Lấy bài học theo khóa học (Student đã mua hoặc Instructor)
        [HttpGet("course/{courseId}")]
        [Authorize]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;

            // Kiểm tra quyền truy cập
            if (role == "Student")
            {
                var enrolled = await _db.Enrollments.AnyAsync(e => e.StudentId == userId && e.CourseId == courseId);
                if (!enrolled) return Forbid();
            }

            var lessons = await _db.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            // Lấy tiến độ nếu là student
            List<LessonDto> result = new();
            foreach (var l in lessons)
            {
                bool completed = false;
                if (role == "Student")
                {
                    var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == courseId);
                    if (enrollment != null)
                        completed = await _db.LessonProgresses.AnyAsync(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id && p.IsCompleted);
                }
                bool hasQuiz = await _db.Quizzes.AnyAsync(q => q.LessonId == l.Id);
                result.Add(new LessonDto
                {
                    Id = l.Id, Title = l.Title, Description = l.Description,
                    VideoUrl = l.VideoUrl, OrderIndex = l.OrderIndex,
                    CourseId = l.CourseId, IsCompleted = completed, HasQuiz = hasQuiz
                });
            }
            return Ok(result);
        }

        // UC-07 (Instructor): Tạo bài học
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Create(CreateLessonDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId && c.InstructorId == userId);
            if (course == null) return Forbid();

            var lesson = new Lesson
            {
                Title = dto.Title, Description = dto.Description,
                VideoUrl = dto.VideoUrl, OrderIndex = dto.OrderIndex,
                CourseId = dto.CourseId
            };
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Tạo bài học thành công!", lessonId = lesson.Id });
        }

        // UC-07: Cập nhật bài học
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Update(int id, CreateLessonDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lesson = await _db.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null || lesson.Course.InstructorId != userId) return Forbid();

            lesson.Title = dto.Title;
            lesson.Description = dto.Description;
            lesson.VideoUrl = dto.VideoUrl;
            lesson.OrderIndex = dto.OrderIndex;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công." });
        }

        // UC-07: Upload video
        [HttpPost("upload-video")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file");
            if (file.Length > 500 * 1024 * 1024) return BadRequest("File quá 500MB.");

            var uploads = Path.Combine("wwwroot", "videos");
            Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return Ok(new { url = $"/videos/{fileName}" });
        }

        // UC-04 (Student): Đánh dấu bài học hoàn thành
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lesson = await _db.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == lesson.CourseId);
            if (enrollment == null) return Forbid();

            var progress = await _db.LessonProgresses
                .FirstOrDefaultAsync(p => p.EnrollmentId == enrollment.Id && p.LessonId == id);

            if (progress == null)
            {
                _db.LessonProgresses.Add(new LessonProgress
                {
                    EnrollmentId = enrollment.Id, LessonId = id,
                    IsCompleted = true, CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }

            // Check nếu hoàn thành 100%
            var totalLessons = await _db.Lessons.CountAsync(l => l.CourseId == lesson.CourseId);
            var completedLessons = await _db.LessonProgresses
                .CountAsync(p => p.EnrollmentId == enrollment.Id && p.IsCompleted);

            bool courseCompleted = (completedLessons + 1) >= totalLessons;
            if (courseCompleted)
            {
                enrollment.IsCompleted = true;
                enrollment.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đánh dấu hoàn thành.", courseCompleted });
        }
    }
}
