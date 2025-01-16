using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Events.DTO;

public class EventDto
{
    public string EventName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime StartEventTime { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime EndventDateTime { get; set; }

    public string adress { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    
    public int MaxEventParticipantNumber { get; set; } 
    
    public string City { get; set; } = string.Empty;
    
    [FromForm]
    public IFormFile Photo { get; set; } // Fotoğraf dosyası için alan
}