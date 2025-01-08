using System.Text.Json.Serialization;

namespace Events.Models;

public class Course
{
    public int Id { get; set; }
    
    public string CourseName { get; set; } = string.Empty;
    
    public string CourseDescription { get; set; } = string.Empty;

    public DateTime StartCourseTime { get; set; }
    
    public DateTime EndCourseDateTime { get; set; }

    public string CourseAdress { get; set; } = string.Empty;
    
    public string CourseCity { get; set; } = string.Empty;

    public string CourseCreator { get; set; } = string.Empty; //silinecek 

    public string CourseCategory { get; set; } = string.Empty;
    
    public string CourseStatus { get; set; } = string.Empty; // "Aktif", "Pasif", "Tamamlandı"
    
    public int UserId { get; set; }
    
    [JsonIgnore]
    public User User { get; set; } = null!;
    
    public ICollection<CourseParticipation> CourseParticipations { get; set; }
}