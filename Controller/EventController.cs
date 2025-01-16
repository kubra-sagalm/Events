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

         // Tarihleri UTC'ye dönüştür
         var startEventTimeUtc = DateTime.SpecifyKind(eventDto.StartEventTime, DateTimeKind.Utc);
         var endventDateTimeUtc = DateTime.SpecifyKind(eventDto.EndventDateTime, DateTimeKind.Utc);

          var newEvent = new Event
          {
            Description = eventDto.Description,
            EndventDateTime = endventDateTimeUtc,
            EventName = eventDto.EventName,
            adress = eventDto.adress,
            Category = eventDto.Category,
            UserId = user.Id,
            MaxEventParticipantNumber = eventDto.MaxEventParticipantNumber,
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
            var events = _context.Events
                .Where(x => x.EventStatus == "Onaylandı")  // EventStatus'u "Onaylandı" olanları al
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


    [Authorize]
    [HttpPatch]
    public ActionResult<Event> UpdateEvent(int EventId, EventDto eventDto)
    {
        try
        {
            var updateEvent = _context.Events.FirstOrDefault(x => x.Id == EventId);
            if (updateEvent == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }
            if(updateEvent.EventStatus == "Onaylandı")
            {
                return BadRequest("Etkinlik onaylandığı için güncelleme yapılamaz.");
            }
            if(updateEvent.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            
            updateEvent.Description = eventDto.Description;
            updateEvent.EndventDateTime = eventDto.EndventDateTime;
            updateEvent.EventName = eventDto.EventName;
            updateEvent.adress = eventDto.adress;
            updateEvent.Category = eventDto.Category;
            updateEvent.MaxEventParticipantNumber = eventDto.MaxEventParticipantNumber;
            updateEvent.StartEventTime = eventDto.StartEventTime;
            updateEvent.City = eventDto.City;
            updateEvent.CreateEventTime = DateTime.UtcNow;
            _context.Update(updateEvent);
            _context.SaveChanges();
            return Ok(updateEvent);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
            
    
    
    
    
}