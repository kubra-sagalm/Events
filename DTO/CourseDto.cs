namespace Events.DTO;

public class CourseDto
{
    public string CourseName { get; set; } = string.Empty;
    
    public string CourseDescription { get; set; } = string.Empty;

    public DateTime StartCourseTime { get; set; }
    
    public DateTime EndCourseDateTime { get; set; }

    public string CourseAdress { get; set; } = string.Empty;

    public string CourseCategory { get; set; } = string.Empty;
    
    public string CourseCity { get; set; } = string.Empty;

}