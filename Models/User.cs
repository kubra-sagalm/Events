using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Events.Models;

public class User
{
    public int Id { get; set; } // Otomatik artan birincil anahtar
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    public Boolean Gender { get; set; }
    
    public string PhoneNumber { get; set; } 
    
    public string Role { get; set; } = string.Empty; // "Admin", "User","yönetici"
    
    public string Password { get; set; } = string.Empty; // Şifreyi hashleyerek
 
    [JsonIgnore]
    public ICollection<Event> Events { get; set; } = new List<Event>();
    
    [JsonIgnore]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    
    public ICollection<CourseParticipation> CourseParticipations { get; set; }
    
    public ICollection<EventParticipation> EventParticipations { get; set; }
    public string? ProfilePhotoPath { get; set; }
}