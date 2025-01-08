using System.Text.Json.Serialization;

namespace Events.Models;

public class CourseParticipation
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }  // User ile ilişki

    public int CourseId { get; set; }
    [JsonIgnore]
    public Course Course { get; set; }
    
    public DateTime ParticipationTime { get; set; }
    
    public string Status { get; set; }  // "Katıldı", "Bekliyor", vb.
    
}