using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Events.Models;

public class EventParticipation
{
    [Key]
    public int Id { get; set; } 
    
    public int UserId { get; set; }
    public User User { get; set; } 
    
    public int EventId { get; set; }
    
    
    public DateTime ParticipationTime { get; set; } 
    
    public string Status { get; set; }  // "Katıldı", "Bekliyor", vb.
    
    [JsonIgnore]
    public virtual Event Event { get; set; }
    
}