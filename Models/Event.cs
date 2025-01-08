using System.Text.Json.Serialization;

namespace Events.Models;

public class Event
{
    public int Id { get; set; }
    
    public string EventName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    public DateTime StartEventTime { get; set; }
    
    public DateTime EndventDateTime { get; set; }
    
    public DateTime CreateEventTime { get; set; } = DateTime.Now;

    public string adress { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public int MaxEventParticipantNumber { get; set; } 
    
    public int EventParticipantNumber { get; set; }
    
    public string EventStatus { get; set; } = string.Empty; // "Aktif", "Pasif", "Tamamlandı"
    public int UserId { get; set; }
    [JsonIgnore]
    public User User { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<EventParticipation> EventParticipations { get; set; }
}