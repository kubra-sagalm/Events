namespace Events.Models;

public class EventParticipation
{
    public int Id { get; set; } 
    
    public int UserId { get; set; }
    
    public int EventId { get; set; }
    
    public string Status { get; set; }  // "Katıldı", "Bekliyor", vb.
    
    public virtual Event Event { get; set; }
}