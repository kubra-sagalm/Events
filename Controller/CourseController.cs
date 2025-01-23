using System.Security.Claims;
using Events.DbContext;
using Events.DTO;
using Events.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Events.Controller;

[Route("api/[controller]")]
[ApiController]
public class CourseController : ControllerBase
{
    public ApplicationDbContext _context;

    public CourseController(ApplicationDbContext context)
    {
        this._context = context;
    }
    
    // Aktif tüm kurslar listelenir

    [Authorize]
    [HttpGet("/ActiveAllCourse")]
    public ActionResult<List<Course>> GetActiveCourse()
    {
        try
        {
            // Kullanıcının kimliğini al
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kursları al
            var courses = _context.Courses
                .Where(course => course.CourseStatus == "Onaylandı" && // Sadece onaylanmış kurslar
                                 !_context.CourseParticipations // Kullanıcının ilişkili olduğu kursları hariç tut
                                     .Where(p => p.UserId == userId) // Kullanıcıya ait katılımlar
                                     .Any(p => p.CourseId == course.Id)) // Kullanıcının katıldığı kursları hariç tut
                .ToList();

            if (courses == null || !courses.Any())
            {
                return NotFound("Aktif kurs bulunamadı.");
            }

            return Ok(courses);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    
    
    // kullanıcının kendi açtığı tüm kurslar listelenir
    [Authorize]
    [HttpGet("/AllCourse")]
    public ActionResult<List<object>> GetAllCourse()
    {
        try
        {
            // Kullanıcının kimlik bilgisinden ID alınıyor
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı kimliği bulunamadı.");
            }

            int id;
            if (!int.TryParse(userIdClaim.Value, out id))
            {
                return BadRequest("Geçersiz kullanıcı ID'si.");
            }

            // Kullanıcı kontrolü
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcının kursları
            var courses = _context.Courses
                .Where(x => x.UserId == id)
                .Select(cp => new
                {
                    Id = cp.Id,
                    CourseName = cp.CourseName,
                    CourseDescription = cp.CourseDescription,
                    StartCourseTime = cp.StartCourseTime,
                    EndCourseDateTime = cp.EndCourseDateTime,
                    CourseAdress = cp.CourseAdress,
                    CourseCity = cp.CourseCity,
                    CourseCategory = cp.CourseCategory,
                    CourseStatus = cp.CourseStatus,
                    PhotoUrl = string.IsNullOrEmpty(cp.PhotoUrl)
                        ? "https://dummyimage.com/600x400/cccccc/ffffff&text=No+Image" // Varsayılan görsel
                        : $"http://localhost:5287{cp.PhotoUrl}" // Tam URL formatında döner
                })
                .ToList();

            // Kullanıcıya ait kurs yoksa
            if (!courses.Any())
            {
                return NotFound("Kullanıcıya ait kurs bulunamadı.");
            }

            return Ok(courses);
        }
        catch (Exception e)
        {
            // Hata loglama ve 500 döndürme
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }



    
    [Authorize]
[HttpPost]
public async Task<ActionResult<Course>> CreateCourse(CourseDto courseDto)
{
    try
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Kullanıcı bilgisi bulunamadı.");
        }

        int userId = int.Parse(userIdClaim.Value);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return Unauthorized("Geçersiz kullanıcı.");
        }

        string photoPath = null;

        // Fotoğraf yükleme işlemi
        if (courseDto.Photo != null && courseDto.Photo.Length > 0)
        {
            var uploadsFolder = Path.Combine("wwwroot", "course-photos");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + courseDto.Photo.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await courseDto.Photo.CopyToAsync(fileStream);
            }

            photoPath = $"/course-photos/{uniqueFileName}";
        }

        // Tarihleri UTC'ye dönüştür
        var startCourseTimeUtc = DateTime.SpecifyKind(courseDto.StartCourseTime, DateTimeKind.Utc);
        var endCourseDateTimeUtc = DateTime.SpecifyKind(courseDto.EndCourseDateTime, DateTimeKind.Utc);

        var newCourse = new Course
        {
            CourseName = courseDto.CourseName,
            CourseCategory = courseDto.CourseCategory,
            CourseAdress = courseDto.CourseAdress,
            CourseCity = courseDto.CourseCity,
            CourseDescription = courseDto.CourseDescription,
            UserId = user.Id,
            CourseCreator = user.FirstName,
            StartCourseTime = startCourseTimeUtc,
            EndCourseDateTime = endCourseDateTimeUtc,
            CourseStatus = user.Role == "Admin" ? "Onaylandı" : "Adminden Onay Bekliyor",
            PhotoUrl = photoPath
        };

        _context.Courses.Add(newCourse);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateCourse), new { id = newCourse.Id }, newCourse);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Hata oluştu: {e.Message}");
        return StatusCode(500, "Internal server error");
    }
}

    
  [Authorize]
[HttpGet("MyCourseParticipations")]
public ActionResult<List<object>> GetMyCourseParticipations()
{
    try
    {
        // Kullanıcının kimlik bilgisinden UserId alınıyor
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("Kullanıcı kimliği bulunamadı.");
        }

        // userId'yi int'e dönüştür
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return BadRequest("Kullanıcı kimliği geçersiz.");
        }

        // Sisteme giriş yapan kullanıcıya ait kurs katılımlarını getir
        var myCourseParticipations = _context.CourseParticipations
            .Where(cp => cp.UserId == userId) // Kullanıcıya ait katılımlar
            .Select(cp => new
            {
                Course = new
                {
                    Id = cp.Course.Id,                   // Kurs ID'si
                    CourseName = cp.Course.CourseName,   // Kurs adı
                    Description = cp.Course.CourseDescription, // Kurs açıklaması
                    StartCourseTime = cp.Course.StartCourseTime, // Başlangıç tarihi
                    EndCourseTime = cp.Course.EndCourseDateTime,     // Bitiş tarihi
                    Instructor = cp.Course.CourseCreator,  // Eğitmen bilgisi
                    Category = cp.Course.CourseCategory,      // Kategori
                    CourseStatus = cp.Course.CourseStatus, // Kurs durumu
                    PhotoUrl = string.IsNullOrEmpty(cp.Course.PhotoUrl)
                        ? "https://dummyimage.com/600x400/cccccc/ffffff&text=No+Image" // Varsayılan görsel
                        : $"http://localhost:5287{cp.Course.PhotoUrl}" // Tam URL formatında döner
                },
                Status = cp.Status // Durum bilgisi: Onaylandı, Reddedildi, Bekliyor
            })
            .ToList<object>(); // Türü açıkça belirtiliyor

        return Ok(myCourseParticipations);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Hata oluştu: {e.Message}");
        return StatusCode(500, "Internal server error");
    }
}



    [Authorize]
    [HttpDelete("{CourseId}")]

    public ActionResult<Course> DeleteCourse(int CourseId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int id = int.Parse(userIdClaim.Value);
            
            User user = _context.Users.FirstOrDefault(x => x.Id == id);
            if(user.Role == "Admin")
            {
                var AdminDeleteCourses = _context.Courses.FirstOrDefault(x => x.Id == id);

                if (AdminDeleteCourses == null)
                {
                    return Ok("kurs bulunamadı");
                }

                _context.Courses.Remove(AdminDeleteCourses);
                _context.SaveChanges();
                return Ok("Kurs silindi");
            }

            var course = _context.Courses
                .Where(x => x.UserId == id).ToList();
            
            var DeleteCourse = course.FirstOrDefault(x => x.Id == CourseId);
            if (DeleteCourse == null)
            {
                return NotFound("Kurs bulunamadı");
            }
            _context.Courses.Remove(DeleteCourse);
            _context.SaveChanges();
            return Ok("Kurs silindi");



        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }
    [Authorize]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelCourse(int id)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course == null)
        {
            return NotFound("Kurs bulunamadı.");
        }

        // İptal durumunu güncelle
        course.CourseStatus = "İptal Edildi";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Kurs başarıyla iptal edildi." });
    }
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromForm] CourseDto courseDto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound("Kurs bulunamadı");
        }

        // Kurs bilgilerini güncelle
        course.CourseName = courseDto.CourseName;
        course.CourseCategory = courseDto.CourseCategory;
        course.CourseAdress = courseDto.CourseAdress;
        course.CourseCity = courseDto.CourseCity;
        course.CourseDescription = courseDto.CourseDescription;

        // Fotoğraf güncellemesi
        if (courseDto.Photo != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + courseDto.Photo.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await courseDto.Photo.CopyToAsync(stream);
            }

            // Eski fotoğrafı sil
            if (!string.IsNullOrEmpty(course.PhotoUrl))
            {
                var oldPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", course.PhotoUrl);
                if (System.IO.File.Exists(oldPhotoPath))
                {
                    System.IO.File.Delete(oldPhotoPath);
                }
            }

            // Yeni fotoğraf yolunu kaydet
            course.PhotoUrl = $"/uploads/{uniqueFileName}"; // Yolu düzelt
        }

        await _context.SaveChangesAsync();

        return Ok(course);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);  // Kursu ID'ye göre al
        if (course == null)
        {
            return NotFound("Kurs bulunamadı.");
        }
        return Ok(course);  // Kurs bilgilerini geri gönder
    }

    
    
    

    
}