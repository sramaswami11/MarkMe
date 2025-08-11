namespace MarkMe.Models;

public class ClassAttendance
{
    public string ClassName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<StudentAttendance> Students { get; set; } = new();

}
