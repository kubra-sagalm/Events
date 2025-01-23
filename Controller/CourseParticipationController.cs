using System.Security.Claims;
using Events.DbContext;
using Events.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Events.Controller;


[ApiController]
[Route("api/[controller]")]
public class CourseParticipationController: ControllerBase
{
    public ApplicationDbContext _context;

    public CourseParticipationController(ApplicationDbContext context)
    {
        this._context = context;
    }
    
    //Kullanıcı istek gönderir
    [Authorize]
    [HttpPost]
    public ActionResult<Course> RequestCourseParticipation(int courseId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }
            int userId = int.Parse(userIdClaim.Value);
            
            var course =_context.Courses.FirstOrDefault(x => x.Id == courseId);
            if (course == null)
            {
                return NotFound("Kurs bulunamadı.");
            }
            
            var existingParticipation = _context.CourseParticipations
                .FirstOrDefault(cp => cp.UserId == userId && cp.CourseId == courseId);
            if (existingParticipation != null)
            {
                return Unauthorized("Kullanıcı zaten bu kursa katılmış.");
            }
            
            var courseParticipation = new CourseParticipation
            {
                UserId = userId,
                CourseId = courseId,
                ParticipationTime = DateTime.UtcNow,
                Status = "Kurs sahbinden onay bekliyor"
            };
            _context.CourseParticipations.Add(courseParticipation);
            _context.SaveChanges();
            return Ok(courseParticipation);
            

        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        
    }
    
    
    [Authorize]
    [HttpDelete]
    public ActionResult DeleteCourseParticipationRequest(int courseId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }
            int userId = int.Parse(userIdClaim.Value);

            var participation = _context.CourseParticipations
                .FirstOrDefault(cp => cp.UserId == userId && cp.CourseId == courseId);
        
            if (participation == null)
            {
                return NotFound("Silinecek kurs katılım isteği bulunamadı.");
            }

            _context.CourseParticipations.Remove(participation);
            _context.SaveChanges();

            return Ok("Kurs katılım isteği başarıyla silindi.");
        }
        catch (Exception e)
        {
            return BadRequest($"Bir hata oluştu: {e.Message}");
        }
    }

    //Kurs sahibi istği onaylar 
    [Authorize]
    [HttpPost("Approve")]
    public ActionResult<Course> CourseParticipationApprove(int courseParticipationId)
    {
        try
        {
            var courseParticipation = _context.CourseParticipations.Include(cp => cp.Course).FirstOrDefault(x=>x.Id == courseParticipationId);
            if (courseParticipation == null)
            {
                return NotFound("Kurs katılımı bulunamadı.");
            }
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }
            int userId = int.Parse(userIdClaim.Value);
            if (userId != courseParticipation.Course.UserId)
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            courseParticipation.Status = "Onaylandı";
            _context.SaveChanges();
            return Ok(courseParticipation);

        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    //Kurs sahibi istği reddeder
    [Authorize]
    [HttpPost("Reject")]
    public ActionResult<Course> CourseParticipationReject(int courseParticipationId)
    {
        try
        {
            var courseParticipation = _context.CourseParticipations.Include(cp => cp.Course).FirstOrDefault(x=>x.Id == courseParticipationId);
            if (courseParticipation == null)
            {
                return NotFound("Kurs katılımı bulunamadı.");
            }
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }
            int userId = int.Parse(userIdClaim.Value);
            if (userId != courseParticipation.Course.UserId)
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            courseParticipation.Status = "Reddedildi";
            _context.SaveChanges();
            return Ok(courseParticipation);

        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    
    [Authorize]
    [HttpGet("courses/{courseId}/participation-requests")]
    public ActionResult<IEnumerable<object>> GetParticipationRequestsByCourseId(int courseId)
    {
        try
        {
            // Kullanıcı bilgilerini al
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest("Geçersiz kullanıcı ID'si.");
            }

            // Kursun sahibi olup olmadığını kontrol et
            var course = _context.Courses.FirstOrDefault(c => c.Id == courseId && c.UserId == userId);
            if (course == null)
            {
                return NotFound("Kurs bulunamadı veya kullanıcı bu kursun sahibi değil.");
            }

            // Katılım isteklerini al
            var participationRequests = _context.CourseParticipations
                .Where(cp => cp.CourseId == courseId)
                .Select(cp => new
                {
                    cp.Id,
                    cp.User.FirstName,
                    cp.User.LastName,
                    cp.User.Email,
                    cp.User.PhoneNumber,
                    cp.Status // Onaylandı, Reddedildi, Bekliyor
                })
                .ToList();

            if (!participationRequests.Any())
            {
                return NotFound("Bu kurs için herhangi bir katılım isteği bulunamadı.");
            }

            return Ok(participationRequests);
        }
        catch (Exception e)
        {
            return BadRequest($"Bir hata oluştu: {e.Message}");
        }
    }



    
    
    
    
    
    
}