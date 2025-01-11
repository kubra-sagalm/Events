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
            var newEvent = new Event
            {
                Description = eventDto.Description,
                EndventDateTime = eventDto.EndventDateTime,
                EventName = eventDto.EventName,
                adress = eventDto.adress,
                Category = eventDto.Category,
                UserId = user.Id,  
                MaxEventParticipantNumber = eventDto.MaxEventParticipantNumber, 
                EventParticipantNumber = 0,
                CreateEventTime = DateTime.UtcNow,
                StartEventTime = eventDto.StartEventTime,
                City = eventDto.City,
                EventStatus = "Adminden Onay Bekliyor"  //Bunun adminden onaylama panaeli olacak unutma
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
    
    //kullanıcının kendi etkinliklerini listeleme
    [Authorize]
    [HttpGet("/AllEvents")]
    public ActionResult<List<Event>> GetEvents()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int id = int.Parse(userIdClaim.Value); 
            
            var user = _context.Users.FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı."); 
            }
            
            var events = _context.Events.Where(x => x.UserId == id ).ToList();

            if (events == null || !events.Any())
            {
                return NotFound("Kullanıcıya ait event bulunamadı."); 
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
    
    //Kullanıcının katıldığı etkinlikler listelenir
    [Authorize]
    [HttpGet("MyEventParticipations")]
    public ActionResult<List<Event>> MyEventParticipations()
    {
        try
        {
            var MyEventParticipations = _context.EventParticipations
                .Where(x => x.UserId == x.UserId && x.Status == "Onaylı")
                .Select(x => x.Event)
                .ToList();
            return (MyEventParticipations);

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