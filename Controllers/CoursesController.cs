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
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CoursesController(AppDbContext db) => _db = db;

        // UC-02: Xem danh sách khóa học (public)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? categoryId)
        {
            var query = _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Where(c => c.Status == "Published")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Title.Contains(search) || c.Description.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(c => c.CategoryId == categoryId.Value);

            var courses = await query.Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Price = c.Price,
                ThumbnailUrl = c.ThumbnailUrl,
                Status = c.Status,
                CategoryName = c.Category.Name,
                CategoryId = c.CategoryId,
                InstructorName = c.Instructor.FullName,
                InstructorId = c.InstructorId,
                TotalLessons = c.Lessons.Count,
                TotalEnrollments = c.Enrollments.Count,
                CreatedAt = c.CreatedAt
            }).ToListAsync();

            return Ok(courses);
        }

        // UC-02: Xem chi tiết 1 khóa học
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (c == null) return NotFound();

            return Ok(new
            {
                course = new CourseDto
                {
                    Id = c.Id, Title = c.Title, Description = c.Description,
                    Price = c.Price, ThumbnailUrl = c.ThumbnailUrl, Status = c.Status,
                    CategoryName = c.Category.Name, CategoryId = c.CategoryId,
                    InstructorName = c.Instructor.FullName, InstructorId = c.InstructorId,
                    TotalLessons = c.Lessons.Count, TotalEnrollments = c.Enrollments.Count,
                    CreatedAt = c.CreatedAt
                },
                lessons = c.Lessons.Select(l => new { l.Id, l.Title, l.OrderIndex })
            });
        }

        // UC-07 (Instructor): Tạo khóa học
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Create(CreateCourseDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                ThumbnailUrl = dto.ThumbnailUrl,
                CategoryId = dto.CategoryId,
                InstructorId = userId,
                Status = "Draft"
            };
            _db.Courses.Add(course);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Tạo khóa học thành công!", courseId = course.Id });
        }

        // UC-07 (Instructor): Cập nhật khóa học
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Update(int id, CreateCourseDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);
            if (course == null) return NotFound();

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.Price = dto.Price;
            course.ThumbnailUrl = dto.ThumbnailUrl;
            course.CategoryId = dto.CategoryId;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công." });
        }

        // UC-07: Submit để Admin duyệt
        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Submit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);
            if (course == null) return NotFound();
            course.Status = "Pending";
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã gửi yêu cầu duyệt." });
        }

        // UC-07 (Instructor): Lấy danh sách khóa học của mình
        [HttpGet("my-courses")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> MyCourses()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var courses = await _db.Courses
                .Include(c => c.Category)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Where(c => c.InstructorId == userId)
                .Select(c => new CourseDto
                {
                    Id = c.Id, Title = c.Title, Description = c.Description,
                    Price = c.Price, ThumbnailUrl = c.ThumbnailUrl, Status = c.Status,
                    RejectReason = c.RejectReason,
                    CategoryName = c.Category.Name, CategoryId = c.CategoryId,
                    InstructorName = "", InstructorId = userId,
                    TotalLessons = c.Lessons.Count, TotalEnrollments = c.Enrollments.Count,
                    CreatedAt = c.CreatedAt
                }).ToListAsync();
            return Ok(courses);
        }

        // UC-08 (Admin): Duyệt khóa học
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();
            course.Status = "Published";
            await _db.SaveChangesAsync();
            return Ok(new { message = "Khóa học đã được duyệt và xuất bản." });
        }

        // UC-08 (Admin): Từ chối khóa học
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, [FromBody] string reason)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();
            course.Status = "Rejected";
            course.RejectReason = reason;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối khóa học." });
        }

        // Admin: Lấy tất cả (kể cả pending)
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdmin()
        {
            var courses = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Select(c => new CourseDto
                {
                    Id = c.Id, Title = c.Title, Description = c.Description,
                    Price = c.Price, ThumbnailUrl = c.ThumbnailUrl, Status = c.Status,
                    RejectReason = c.RejectReason,
                    CategoryName = c.Category.Name, CategoryId = c.CategoryId,
                    InstructorName = c.Instructor.FullName, InstructorId = c.InstructorId,
                    TotalLessons = c.Lessons.Count, TotalEnrollments = c.Enrollments.Count,
                    CreatedAt = c.CreatedAt
                }).ToListAsync();
            return Ok(courses);
        }

        // Upload thumbnail
        [HttpPost("upload-thumbnail")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> UploadThumbnail(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file");
            var uploads = Path.Combine("wwwroot", "thumbnails");
            Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return Ok(new { url = $"/thumbnails/{fileName}" });
        }
    }
}
