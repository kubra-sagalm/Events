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
            UserId = user.Id
                
            
        };
        _context.Courses.Add(course);
        _context.SaveChanges();
        return Ok(course);
    }
    
    
    [HttpGet]
    public ActionResult<List<Event>> GetCouses()
    {
        try
        {

            var course = _context.Courses.ToList();

            if (course == null)
            {
                return NotFound("Kurs Bulunmamaktadır.");
                
            }

            return Ok(course);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }
    
    
}