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
    
    [HttpPost ("upload-photo")]
    public ActionResult UploadProfilePhoto(IFormFile file, int userId)
    {
        try
        {
            // Kullanıcının varlığını kontrol et
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("Geçerli bir dosya seçiniz.");
            }

            // Dosya adını benzersiz yap
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";

            // Dosya kaydedileceği yol (wwwroot/profiles dizini)
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/profiles", fileName);

            // wwwroot/profiles klasörünün varlığını kontrol et, yoksa oluştur
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Kullanıcının profil fotoğrafı alanını güncelle
            user.ProfilePhotoPath = $"/profiles/{fileName}";
            _context.Users.Update(user);
            _context.SaveChanges();

            return Ok(new { message = "Profil fotoğrafı başarıyla yüklendi.", photoPath = user.ProfilePhotoPath });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpGet("profile-photo/{userId}")]
    public IActionResult GetProfilePhoto(int userId)
    {
        try
        {
            // Kullanıcıyı veritabanından al
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcının profil fotoğraf yolunu kontrol et
            if (string.IsNullOrEmpty(user.ProfilePhotoPath))
            {
                return NotFound("Profil fotoğrafı bulunamadı.");
            }

            // Fotoğrafın fiziksel yolunu belirle
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePhotoPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Profil fotoğrafı sunucuda bulunamadı.");
            }

            // Fotoğrafı döndür
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "image/jpeg"; // Fotoğrafın içerik tipi (JPEG varsayıldı, gerekirse dinamikleştirin)
            return File(fileBytes, contentType);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Hata oluştu: {e.Message}");
            return StatusCode(500, "Sunucu hatası.");
        }
    }

    
    
    
    
    
    
}