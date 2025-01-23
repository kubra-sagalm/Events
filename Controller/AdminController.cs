using System.Security.Claims;
using Events.DbContext;
using Events.DTO;
using Events.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Controller;

[ApiController]
[Microsoft.AspNetCore.Components.Route("api/[controller]")]
public class AdminController: ControllerBase
{
    public ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        this._context = context;
    }

    [Authorize]
    [HttpPost("ApproveEvent/{id}")]
    public ActionResult<Event> ApproveEvent(int id)
    {
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if(user.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            var ApproveEvent = _context.Events.FirstOrDefault(x => x.Id == id);
            if (ApproveEvent == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }
            ApproveEvent.EventStatus = "Onaylandı";
            _context.Update(ApproveEvent);
            _context.SaveChanges();
            return Ok(ApproveEvent);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }


    [Authorize]
    [HttpPost("RejectEvent/{id}")]
    public ActionResult<Event> RejectEvent(int id)
    {
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if(user.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            
            var rejectEvent = _context.Events.FirstOrDefault(x=> x.Id == id);
            if (rejectEvent == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }
            rejectEvent.EventStatus = "Reddedildi";
            _context.Update(rejectEvent);
            _context.SaveChanges();
            return Ok(rejectEvent);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpGet("PendingEvents")]
    public ActionResult<List<object>> PedingEvent()
    {
        try
        { 
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if(user.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            var pendingEvents = _context.Events
                .Where(x => x.EventStatus == "Adminden Onay Bekliyor")
                .Select(e => new 
                {
                    e.Id,
                    e.EventName,
                    e.EndventDateTime,
                    EventOwner = _context.Users.Where(u => u.Id == e.UserId).Select(u => new 
                    {
                        u.FirstName,
                        u.LastName // Ek olarak soyadı eklenebilir.
                    }).FirstOrDefault(),
                    e.EventStatus,
                    User = _context.Users.Where(u => u.Id == e.UserId).Select(u => new 
                    {
                        u.FirstName,
                        u.Email,
                        u.PhoneNumber
                    }).FirstOrDefault()
                })
                .ToList();


            if (pendingEvents == null || !pendingEvents.Any())
            {
                return NotFound("Onay bekleyen etkinlik bulunamadı.");
            }

            return Ok(pendingEvents);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }


    //Course için işlemler 
    
    
    [Authorize]
    [HttpPost("ApproveCourse/{id}")]
    public ActionResult<Event> ApproveCourse(int id)
    {
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if(user.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            var ApproveCourse = _context.Courses.FirstOrDefault(x => x.Id == id);
            if (ApproveEvent == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }
            ApproveCourse.CourseStatus = "Onaylandı";
            _context.Update(ApproveCourse);
            _context.SaveChanges();
            return Ok(ApproveCourse);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }


    [Authorize]
    [HttpPost("RejectCourse/{id}")]
    public ActionResult<Event> RejectCourse(int id)
    {
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if(user.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            
            var rejectCourse = _context.Courses.FirstOrDefault(x=> x.Id == id);
            if (rejectCourse == null)
            {
                return NotFound("Kurs bulunamadı.");
            }
            rejectCourse.CourseStatus = "Reddedildi";
            _context.Update(rejectCourse);
            _context.SaveChanges();
            return Ok(rejectCourse);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpGet("PendingCourses")]
    public ActionResult<List<object>> PedingCourses()
    {
        try
        { 
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null || user.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
        
            var pendingCourses = _context.Courses
                .Where(x => x.CourseStatus == "Adminden Onay Bekliyor")
                .Select(course => new 
                {
                    course.Id,
                    course.CourseName,
                    course.StartCourseTime,
                    course.EndCourseDateTime,
                    OwnerName = _context.Users.Where(u => u.Id == course.UserId).Select(u => u.FirstName).FirstOrDefault() ?? "Bilinmiyor",
                    OwnerInfo = new
                    {
                        Email = _context.Users.Where(u => u.Id == course.UserId).Select(u => u.Email).FirstOrDefault() ?? "Bilinmiyor",
                        Phone = _context.Users.Where(u => u.Id == course.UserId).Select(u => u.PhoneNumber).FirstOrDefault() ?? "Bilinmiyor"
                    }
                })
                .ToList();

            if (!pendingCourses.Any())
            {
                return NotFound("Onay bekleyen kurs bulunamadı.");
            }

            return Ok(pendingCourses);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    
    [Authorize]
    [HttpPost("AddAdmin")]
    public ActionResult<User> createAdmin(UserDto userDto)
    {
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user1 = _context.Users.FirstOrDefault(x => x.Id == userId);
            if(user1.Role != "Admin")
            {
                return Unauthorized("Bu işlemi yapmaya yetkiniz yok.");
            }
            
            var user = new User
            {
               FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                Role = "Admin",
                Age = userDto.Age,
                PhoneNumber = userDto.PhoneNumber
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(user);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
    
        
        
        
        
        
        
        
}