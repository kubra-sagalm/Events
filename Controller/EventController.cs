using System.Security.Claims;
using Events.DbContext;
using Events.DTO;
using Events.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Events.Controller;

[ApiController]
[Route("api/[controller]")]
public class EventController: ControllerBase
{
    
    public ApplicationDbContext _context;

    public EventController(ApplicationDbContext context)
    {
        this._context = context;
    }
    
 [Authorize]
[HttpPost]
public async Task<ActionResult<Event>> CreateEvent(EventDto eventDto)
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
        if (eventDto.Photo != null && eventDto.Photo.Length > 0)
        {
            var uploadsFolder = Path.Combine("wwwroot", "event-photos");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + eventDto.Photo.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await eventDto.Photo.CopyToAsync(fileStream);
            }

            photoPath = $"/event-photos/{uniqueFileName}";
        }

        // Tarihlerin null olup olmadığını kontrol edin
        if (!eventDto.StartEventTime.HasValue || !eventDto.EndventDateTime.HasValue)
        {
            return BadRequest("Etkinlik başlangıç ve bitiş tarihleri zorunludur.");
        }

        // Tarihleri UTC'ye dönüştür
        var startEventTimeUtc = DateTime.SpecifyKind(eventDto.StartEventTime.Value, DateTimeKind.Utc);
        var endventDateTimeUtc = DateTime.SpecifyKind(eventDto.EndventDateTime.Value, DateTimeKind.Utc);

        var newEvent = new Event
        {
            Description = eventDto.Description,
            EndventDateTime = endventDateTimeUtc,
            EventName = eventDto.EventName,
            adress = eventDto.adress,
            Category = eventDto.Category,
            UserId = user.Id,
            MaxEventParticipantNumber = eventDto.MaxEventParticipantNumber ?? 0, // Null ise 0 atanır
            EventParticipantNumber = 0,
            CreateEventTime = DateTime.UtcNow, // Zaten UTC
            StartEventTime = startEventTimeUtc,
            City = eventDto.City,
            EventStatus = user.Role == "Admin" ? "Onaylandı" : "Adminden Onay Bekliyor",
            PhotoUrl = photoPath
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateEvent), new { id = newEvent.Id }, newEvent);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Hata oluştu: {e.Message}");
        return StatusCode(500, "Internal server error");
    }
}

    
   [Authorize]
[HttpGet("/AllEvents")]
public ActionResult<List<object>> GetEvents()
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

        // Kullanıcının etkinlikleri
        var events = _context.Events
            .Where(x => x.UserId == id)
            .Select(e => new
            {
                Id = e.Id,
                EventName = e.EventName,
                Description = e.Description,
                StartEventTime = e.StartEventTime,
                EndEventTime = e.EndventDateTime,
                Adress = e.adress,
                City = e.City,
                Category = e.Category,
                EventParticipantNumber = e.EventParticipantNumber,
                MaxEventParticipantNumber = e.MaxEventParticipantNumber,
                EventStatus = e.EventStatus,
                PhotoUrl = string.IsNullOrEmpty(e.PhotoUrl)
                    ? "https://dummyimage.com/600x400/cccccc/ffffff&text=No+Image" // Varsayılan bir görsel döner
                    : $"http://localhost:5287{e.PhotoUrl}" // Tam URL formatında döner
            })
            .ToList();

        // Kullanıcıya ait etkinlik yoksa
        if (!events.Any())
        {
            return NotFound("Kullanıcıya ait etkinlik bulunamadı.");
        }

        return Ok(events);
    }
    catch (Exception e)
    {
        // Hata loglama ve 500 döndürme
        Console.WriteLine($"Hata oluştu: {e.Message}");
        return StatusCode(500, "Internal server error");
    }
}



    [Authorize]
    [HttpDelete("Event/Delete/{EventId}")]
    public ActionResult<Event> DeleteEvent(int EventId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int id = int.Parse(userIdClaim.Value);
            
            User user = _context.Users.FirstOrDefault(x => x.Id == id);
            if(user.Role == "Admin")
            {
                var AdminDeleteEvents = _context.Events.FirstOrDefault(x => x.Id == EventId);

                if (AdminDeleteEvents == null)
                {
                    return Ok("etkinlik bulunamadı");
                }

                _context.Events.Remove(AdminDeleteEvents);
                _context.SaveChanges();

                return Ok("Silme işlemi başarıyla gerçekleştirildi.");
            }
            var events = _context.Events.Where(x => x.UserId == id).ToList();

            var DeleteEvents = events.FirstOrDefault(x => x.Id == EventId);

            if (DeleteEvents == null)
            {
                return Ok("etkinlik bulunamadı");
            }

            _context.Events.Remove(DeleteEvents);
            _context.SaveChanges();

            return Ok("Silme işlemi başarıyla gerçekleştirildi.");

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }
    
    [Authorize]
    [HttpGet("ActiveAllEvents")]
    public ActionResult<List<Event>> GetEventsStatus()
    {
        try
        {
            // Kullanıcının kimliğini al
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Etkinlikleri al
            var events = _context.Events
                .Where(e => e.EventStatus == "Onaylandı" && // "Onaylandı" durumundaki etkinlikler
                            e.UserId != userId && // Kullanıcının sahibi olduğu etkinlikler hariç
                            !_context.EventParticipations
                                .Where(p => p.UserId == userId) // Kullanıcının katılım bilgilerini filtrele
                                .Any(p => p.EventId == e.Id)) // Kullanıcının bu etkinliğe katılıp katılmadığını kontrol et
                .ToList();

            if (events == null || !events.Any())
            {
                return NotFound("Geçerli etkinlik bulunamadı.");
            }

            return Ok(events);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }




    
[Authorize]
[HttpGet("MyEventParticipations")]
public ActionResult<List<object>> MyEventParticipations()
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

        // Sisteme giriş yapan kullanıcıya ait etkinlik katılımlarını getir
        var myEventParticipations = _context.EventParticipations
            .Where(x => x.UserId == userId) // Kullanıcıya ait katılımlar
            .Select(x => new
            {
                Event = new
                {
                    Id = x.Event.Id,                  // Etkinlik ID'si
                    EventName = x.Event.EventName,    // Etkinlik adı
                    Description = x.Event.Description, // Etkinlik açıklaması
                    StartEventTime = x.Event.StartEventTime, // Başlangıç tarihi
                    EndEventTime = x.Event.EndventDateTime,     // Bitiş tarihi
                    Adress = x.Event.adress,          // Adres
                    City = x.Event.City,              // Şehir
                    Category = x.Event.Category,      // Kategori
                    EventStatus = x.Event.EventStatus,// Etkinlik durumu
                    EventParticipantNumber = x.Event.EventParticipantNumber, // Katılımcı sayısı
                    MaxEventParticipantNumber = x.Event.MaxEventParticipantNumber, // Maksimum katılımcı
                    PhotoUrl = string.IsNullOrEmpty(x.Event.PhotoUrl)
                        ? "https://dummyimage.com/600x400/cccccc/ffffff&text=No+Image" // Varsayılan görsel
                        : $"http://localhost:5287{x.Event.PhotoUrl}" // Tam URL formatında döner
                },
                Status = x.Status // Durum bilgisi: Onaylandı, Reddedildi, Bekliyor
            })
            .ToList<object>(); // Türü açıkça belirtiliyor

        return Ok(myEventParticipations);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Hata oluştu: {e.Message}");
        return StatusCode(500, "Internal server error");
    }
}


[HttpPut("{id}")]
public async Task<IActionResult> UpdateCourse(int id, [FromForm] CourseDto courseDto)
{
    try
    {
        // Kursu veritabanında bul
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound("Kurs bulunamadı.");
        }

        // Tüm alanlar için kontrol ekle
        if (string.IsNullOrWhiteSpace(courseDto.CourseName) ||
            string.IsNullOrWhiteSpace(courseDto.CourseCategory) ||
            string.IsNullOrWhiteSpace(courseDto.CourseAdress) ||
            string.IsNullOrWhiteSpace(courseDto.CourseCity) ||
            string.IsNullOrWhiteSpace(courseDto.CourseDescription))
        {
            return BadRequest("Tüm alanlar doldurulmalıdır.");
        }

        // Geçersiz tarih kontrolü
        if (courseDto.StartCourseTime == default || courseDto.EndCourseDateTime == default)
        {
            return BadRequest("Başlangıç ve bitiş tarihleri geçerli olmalıdır.");
        }

        // Alanları güncelle
        course.CourseName = courseDto.CourseName;
        course.CourseCategory = courseDto.CourseCategory;
        course.CourseAdress = courseDto.CourseAdress;
        course.CourseCity = courseDto.CourseCity;
        course.CourseDescription = courseDto.CourseDescription;
        course.StartCourseTime = courseDto.StartCourseTime;
        course.EndCourseDateTime = courseDto.EndCourseDateTime;

        // Fotoğraf güncelleme
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

            // Eski fotoğrafı sil
            if (!string.IsNullOrEmpty(course.PhotoUrl))
            {
                var oldPhotoPath = Path.Combine("wwwroot", course.PhotoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPhotoPath))
                {
                    System.IO.File.Delete(oldPhotoPath);
                }
            }

            course.PhotoUrl = $"/course-photos/{uniqueFileName}";
        }

        // Veritabanına kaydet
        await _context.SaveChangesAsync();
        return Ok(course);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Hata oluştu: {ex.Message}");
        return StatusCode(500, "Bir hata oluştu.");
    }
}

            
    [Authorize]
    [HttpPost("cancel")]
    public async Task<ActionResult> CancelEvent(int eventId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            var eventToCancel = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventToCancel == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }

            // Kullanıcının etkinliği iptal etme yetkisi var mı kontrol et
            if (eventToCancel.UserId != userId)
            {
                return Forbid("Bu etkinliği iptal etme yetkiniz yok.");
            }

            // Etkinlik durumu "İptal Edildi" olarak güncelle
            eventToCancel.EventStatus = "İptal Edildi";
            await _context.SaveChangesAsync();

            return Ok("Etkinlik başarıyla iptal edildi.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    
    
    
}