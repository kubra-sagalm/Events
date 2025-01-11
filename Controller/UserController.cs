using Events.DbContext;
using Events.DTO;
using Events.Models;
using Microsoft.AspNetCore.Mvc;

namespace Events.Controller;


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    
    public ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        this._context = context;
    }
    
    
    [HttpPost]
    public ActionResult<User> AddUser(UserDto userDto)
    {
        try
        {
            var user = new User
            {
                FirstName =userDto.FirstName,
                Email = userDto.Email,
                LastName = userDto.LastName,
                // Parolayı hash'le
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                Age = userDto.Age,
                PhoneNumber = userDto.PhoneNumber,
                Gender = userDto.Gender,  //true kadın false erkek
                Role = "User"
            };

           
            _context.Users.Add(user);
            _context.SaveChanges();  

            
            return Ok(user);
        }
        catch (Exception e)
        {
           
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); // Sunucu hatası
        }
    }
    
    [HttpGet]
    public ActionResult<List<User>> GetUsers()
    {
        try
        {
            var user = _context.Users.ToList();
            return Ok(user);

        }
        catch (Exception e)
        {
            
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }
    
    [HttpGet("{id}")]
    public ActionResult<User> GetUser(int id)
    {
        try
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            return Ok(user);

        }
        catch (Exception e)
        {
            
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }
    
    [HttpDelete("{id}")]
    public ActionResult<User> DeleteUser(int id)
    {
        try
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            _context.Users.Remove(user);
            _context.SaveChanges();
            return Ok("silme işlemi başarılı");
        }
        catch (Exception e)
        {
            
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error"); 
        }
    }

    
    
    
    
    
    
}