using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Events.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Events.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthController(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Kullanıcıyı veritabanından e-posta ile sorgula
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            // Eğer kullanıcı bulunamazsa, yetkisiz erişim hatası döndür
            if (user == null)
            {
                return Unauthorized("Kullanıcı bulunamadı.");
            }

            // Şifreyi doğrulamak için BCrypt'ü kullan
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized("Şifre hatalı.");
            }

            // Kullanıcı doğrulandı, JWT token oluştur
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email), // Kullanıcı e-postası
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Kullanıcı ID'si
                new Claim(ClaimTypes.Role, user.Role) // Kullanıcının rolü
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30), // Token geçerlilik süresi
                signingCredentials: creds);

            // JWT token ve rol bilgisi döndür
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = user.Role // Kullanıcının rolü
            });
        }

    }

    // Login Model
    public class LoginModel
    {
        
        public string Email { get; set; }  // E-posta
        public string Password { get; set; }  // Parola
    }
}
