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
            // Kullanıcının kimliğini al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Etkinliği bul
            var eventToParticipate = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventToParticipate == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }

            // Kullanıcının zaten bu etkinliğe katılıp katılmadığını kontrol et
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.UserId == userId && ep.EventId == eventId);

            if (existingParticipation != null)
            {
                return BadRequest("Bu etkinlikte zaten katılımınız var.");
            }

            // Yeni katılım isteği oluştur
            var participation = new EventParticipation
            {
                UserId = userId,
                EventId = eventId,
                ParticipationTime = DateTime.UtcNow,
                Status = "Bekliyor"  // Varsayılan olarak "Bekliyor" durumu
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
    [HttpPatch("approve-participation/{participationId}")]
    public async Task<ActionResult> ApproveParticipation(int participationId)
    {
        try
        {
            // Kullanıcının kimliğini al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Katılım talebini bul
            var participation = await _context.EventParticipations
                .Include(ep => ep.Event)  // Event bilgilerini dahil et
                .FirstOrDefaultAsync(ep => ep.Id == participationId);

            if (participation == null)
            {
                return NotFound("Katılım talebi bulunamadı.");
            }

            // Etkinlik sahibi mi kontrol et
            if (participation.Event.UserId != userId)
            {
                return Unauthorized("Bu etkinliğin sahibi değilsiniz.");
            }

            // Katılım talebini onayla
            participation.Status = "Onaylı";  // Onaylanmış olarak güncelleniyor
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
    [HttpGet("event-participations/{eventId}")]
    public async Task<ActionResult<List<EventParticipation>>> GetEventParticipations(int eventId)
    {
        try
        {
            // Kullanıcının kimliğini al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Etkinliği bul
            var eventToCheck = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventToCheck == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }

            // Etkinliği oluşturan kişi mi kontrol et
            if (eventToCheck.UserId != userId)
            {
                return Unauthorized("Bu etkinliğin sahibi değilsiniz.");
            }

            // Etkinlikteki katılım taleplerini getir
            var participations = await _context.EventParticipations
                .Where(ep => ep.EventId == eventId)
                .ToListAsync();

            if (participations == null || !participations.Any())
            {
                return NotFound("Katılım talebi bulunamadı.");
            }

            return Ok(participations);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }


    
    
    
}