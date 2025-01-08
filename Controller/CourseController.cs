using System.Security.Claims;
using Events.DbContext;
using Events.DTO;
using Events.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [Authorize]
    [HttpGet("/ActiveAllCourse")]
    public ActionResult<List<Course>> GetActiveCourse()
    {
        try
        {
            var courses = _context.Courses
                .Where(x => x.CourseStatus == "Onaylandı")
                .ToList();
            if(courses == null)
            {
                return NotFound("Aktif kurs bulunamadı");
            }
            return Ok(courses);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }

    [Authorize]
    [HttpGet("/AllCourse")]
    public ActionResult<List<Course>> GetAllCourse()
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
            
            var courses = _context.Courses.Where(x => x.UserId == id ).ToList();

            if (courses == null || !courses.Any())
            {
                return NotFound("Kullanıcıya ait kurs bulunamadı."); 
            }

            return Ok(courses);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [Authorize]
    [HttpPost]
    public ActionResult<Course> CreateCourse(CourseDto courseDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int userId = int.Parse(userIdClaim.Value); 
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        
        if (user == null)
        {
            return NotFound($"User with Id {userId} not found.");
        }
        var userName = user.FirstName;

        var course = new Course()
        {
            CourseAdress = courseDto.CourseAdress,
            CourseCategory = courseDto.CourseCategory,
            CourseName = courseDto.CourseName,
            CourseCreator = userName,
            CourseDescription = courseDto.CourseDescription,
            StartCourseTime = courseDto.StartCourseTime,
            EndCourseDateTime = courseDto.EndCourseDateTime,
            UserId = user.Id,
            CourseCity = courseDto.CourseCity,
            CourseStatus = "Adminden Onay Bekliyor"
                
            
        };
        
        if(user.Role=="Admin")
        {
            course.CourseStatus = "Onaylandı";
        }
        
        _context.Courses.Add(course);
        _context.SaveChanges();
        return Ok(course);
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

    
}