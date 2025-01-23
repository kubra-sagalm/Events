using System.Security.Claims;
using Events.DbContext;
using Events.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Events.Controller;


[ApiController]
[Route("api/[controller]")]
public class EventParticipationController: ControllerBase
{
    public ApplicationDbContext _context;

    public EventParticipationController(ApplicationDbContext context)
    {
        this._context = context;
    }
    
    [Authorize]
    [HttpPost("participate")]
    public async Task<ActionResult> ParticipateInEvent(int eventId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            var eventToParticipate = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventToParticipate == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }
            
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.UserId == userId && ep.EventId == eventId);

            if (existingParticipation != null)
            {
                return BadRequest("Bu etkinlikte zaten katılımınız var.");
            }

            var participation = new EventParticipation
            {
                UserId = userId,
                EventId = eventId,
                ParticipationTime = DateTime.UtcNow,
                Status = "Adminden Onay Bekliyor"  // Varsayılan olarak "Bekliyor" durumu
            };

            _context.EventParticipations.Add(participation);
            await _context.SaveChangesAsync();

            return Ok("Katılım isteği başarıyla gönderildi.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
    
    
    [Authorize]
    [HttpPost("leave")]
    public async Task<ActionResult> LeaveEvent(int eventId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            var eventToLeave = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventToLeave == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }

            // Etkinlik tarihi geçmiş mi kontrol et
            if (eventToLeave.EndventDateTime< DateTime.UtcNow)
            {
                return BadRequest("Etkinlik tarihi geçtiği için çıkış yapılamaz.");
            }

            var participation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.UserId == userId && ep.EventId == eventId);

            if (participation == null)
            {
                return BadRequest("Bu etkinlikte herhangi bir katılımınız bulunmamaktadır.");
            }

            // Katılımı kaldır
            _context.EventParticipations.Remove(participation);
            await _context.SaveChangesAsync();

            return Ok("Etkinlikten başarıyla çıkış yapıldı.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpPatch("approve-participation/{participationId}")]
    public async Task<ActionResult> ApproveParticipation(int participationId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            var participation = await _context.EventParticipations
                .Include(ep => ep.Event)  
                .FirstOrDefaultAsync(ep => ep.Id == participationId);
            
            var eventToCheck = await _context.Events.FirstOrDefaultAsync(e => e.Id == participation.EventId);

            if (participation == null)
            {
                return NotFound("Katılım talebi bulunamadı.");
            }

            if (participation.Event.UserId != userId)
            {
                return Unauthorized("Bu etkinliğin sahibi değilsiniz.");
            }
            
            if (eventToCheck.EventParticipantNumber >= eventToCheck.MaxEventParticipantNumber)
            {
                return BadRequest("Bu etkinlik için maksimum katılımcı sayısına ulaşıldı.");
            }


            participation.Status = "Onaylı";  // Onaylanmış olarak güncelleniyor
            eventToCheck.EventParticipantNumber += 1;  // Etkinlik katılımcı sayısı bir arttırılıyor
            await _context.SaveChangesAsync();

            return Ok("Katılım isteği onaylandı.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [Authorize]
    [HttpPatch("reject-participation/{participationId}")]
    public async Task<ActionResult> RejectParticipation(int participationId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            var participation = await _context.EventParticipations
                .Include(ep => ep.Event)  
                .FirstOrDefaultAsync(ep => ep.Id == participationId);

            if (participation == null)
            {
                return NotFound("Katılım talebi bulunamadı.");
            }

            if (participation.Event.UserId != userId)
            {
                return Unauthorized("Bu etkinliğin sahibi değilsiniz.");
            }

            if (participation.Status == "Reddedildi")
            {
                return BadRequest("Bu katılım talebi zaten reddedilmiş.");
            }

            participation.Status = "Reddedildi"; // Reddedilmiş olarak güncelleniyor
            await _context.SaveChangesAsync();

            return Ok("Katılım isteği reddedildi.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }


    
    [Authorize]
    [HttpGet("event-participations/{eventId}")]
    public async Task<ActionResult<List<EventParticipation>>> GetEventParticipations(int eventId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Etkinliğin varlığını ve sahibini kontrol et
            var eventToCheck = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventToCheck == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }

            if (eventToCheck.UserId != userId)
            {
                return Unauthorized("Bu etkinliğin sahibi değilsiniz.");
            }

            // Katılımcı bilgilerini kullanıcı ile birlikte al
            var participations = await _context.EventParticipations
                .Where(ep => ep.EventId == eventId)
                .Include(ep => ep.User) // Kullanıcı bilgilerini dahil et
                .ToListAsync();

            if (participations == null || !participations.Any())
            {
                return NotFound("Katılım talebi bulunamadı.");
            }

            // Dönüş modelini özelleştirme (isteğe bağlı)
            var result = participations.Select(p => new
            {
                p.Id,
                p.EventId,
                p.UserId,
                User = p.User == null ? null : new
                {
                    p.User.Id,
                    p.User.FirstName,
                    p.User.LastName,
                    p.User.PhoneNumber,
                    p.User.Email
                },
                p.ParticipationTime,
                p.Status
            }).ToList();

            return Ok(result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    
    
    
}