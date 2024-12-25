using System.Text.Json.Serialization;

namespace Events.Models;

public class User
{
    public int Id { get; set; } // Otomatik artan birincil anahtar
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // Şifreyi hashleyerek
 
    [JsonIgnore]
    public ICollection<Event> Events { get; set; } = new List<Event>();
    
    [JsonIgnore]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}