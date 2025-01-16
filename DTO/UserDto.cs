namespace Events.DTO;

public class UserDto
{
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty; // Şifreyi hashleyerek
    
    public int Age { get; set; }
    
    public string PhoneNumber { get; set; }  = string.Empty;
    
    public Boolean Gender { get; set; }
    
    public string? photoPath { get; set; }

    
}