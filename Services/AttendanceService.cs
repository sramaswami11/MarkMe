using MarkMe.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public class AttendanceService
{
    private readonly string byDatePath = "attendance_by_date.json";
    private readonly string byClassPath = "attendance_by_class.json";
    private readonly string classesPath = "classes.json";
    private readonly string classTeachersPath = "class_teachers.json";

    private List<ClassInfo> _classesCache = new();

    public AttendanceService()
    {
        if (!File.Exists(classesPath))
        {
            var defaults = new List<ClassInfo>
            {
                new ClassInfo { ClassName = "Class 1", Students = new List<string>{ "Alice", "Bob", "Charlie", "Diana" } },
                new ClassInfo { ClassName = "Class 2", Students = new List<string>{ "Ethan", "Fiona", "George", "Hannah" } },
                new ClassInfo { ClassName = "Class 3", Students = new List<string>{ "Ivy", "Jack", "Karen", "Liam" } },
            };
            File.WriteAllText(classesPath, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
        }

        if (!File.Exists(classTeachersPath))
        {
            var defaults = new List<ClassTeacherInfo>
            {
                new ClassTeacherInfo { ClassName = "Class 1", Teacher = "Mr. Smith", CoTeacher = "Mrs. Johnson", Description = "Introductory physics" },
                new ClassTeacherInfo { ClassName = "Class 2", Teacher = "Ms. Brown", CoTeacher = "Mr. White", Description = "Mathematics essentials" },
                new ClassTeacherInfo { ClassName = "Class 3", Teacher = "Dr. Green", CoTeacher = "Ms. Blue", Description = "Chemistry basics" },
            };
            File.WriteAllText(classTeachersPath, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
        }

        _classesCache = LoadClassesFromFile();
    }

    private List<ClassInfo> LoadClassesFromFile()
    {
        try
        {
            return File.Exists(classesPath)
                ? JsonSerializer.Deserialize<List<ClassInfo>>(File.ReadAllText(classesPath)) ?? new List<ClassInfo>()
                : new List<ClassInfo>();
        }
        catch
        {
            return new List<ClassInfo>();
        }
    }

    private void SaveClassesToFile(List<ClassInfo> list)
    {
        File.WriteAllText(classesPath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        _classesCache = list;
    }

    public List<string> GetClasses() => LoadClassesFromFile().Select(c => c.ClassName).ToList();

    public List<string> GetStudentsForClass(string className)
    {
        var cls = LoadClassesFromFile().FirstOrDefault(c => c.ClassName == className);
        return cls?.Students ?? new List<string>();
    }

    public List<StudentAttendance> GetAttendanceForDateAndClass(DateTime date, string className)
    {
        var defaultList = GetStudentsForClass(className)
            .Select(name => new StudentAttendance { Name = name, Status = "A" })
            .ToList();

        if (!File.Exists(byDatePath))
            return defaultList;

        var existing = JsonSerializer.Deserialize<Dictionary<string, List<ClassAttendance>>>(File.ReadAllText(byDatePath))
                       ?? new Dictionary<string, List<ClassAttendance>>();

        string dateKey = date.ToString("yyyy-MM-dd");
        if (existing.TryGetValue(dateKey, out var classRecords))
        {
            var record = classRecords.FirstOrDefault(c => c.ClassName == className);
            if (record != null)
            {
                return record.Students.Select(s => new StudentAttendance { Name = s.Name, Status = s.Status }).ToList();
            }
        }

        return defaultList;
    }

    public async Task SaveAttendanceByDate(DateTime date, string className, List<StudentAttendance> students)
    {
        var existing = File.Exists(byDatePath)
            ? JsonSerializer.Deserialize<Dictionary<string, List<ClassAttendance>>>(File.ReadAllText(byDatePath))
            : new Dictionary<string, List<ClassAttendance>>();

        if (existing == null) existing = new Dictionary<string, List<ClassAttendance>>();

        string dateKey = date.ToString("yyyy-MM-dd");
        if (!existing.ContainsKey(dateKey))
            existing[dateKey] = new List<ClassAttendance>();

        existing[dateKey].RemoveAll(c => c.ClassName == className);

        existing[dateKey].Add(new ClassAttendance
        {
            ClassName = className,
            Date = date,
            Students = students.Select(s => new StudentAttendance { Name = s.Name, Status = s.Status }).ToList()
        });

        await File.WriteAllTextAsync(byDatePath, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task SaveAttendanceByClass(DateTime date, string className, List<StudentAttendance> students)
    {
        var existing = File.Exists(byClassPath)
            ? JsonSerializer.Deserialize<Dictionary<string, List<ClassAttendance>>>(File.ReadAllText(byClassPath))
            : new Dictionary<string, List<ClassAttendance>>();

        if (existing == null) existing = new Dictionary<string, List<ClassAttendance>>();

        if (!existing.ContainsKey(className))
            existing[className] = new List<ClassAttendance>();

        existing[className].RemoveAll(c => c.Date == date);

        existing[className].Add(new ClassAttendance
        {
            ClassName = className,
            Date = date,
            Students = students.Select(s => new StudentAttendance { Name = s.Name, Status = s.Status }).ToList()
        });

        await File.WriteAllTextAsync(byClassPath, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
    }

    // --- Class management (classes.json) ---
    public List<ClassInfo> GetAllClasses()
    {
        return LoadClassesFromFile();
    }

    public void AddClass(string className)
    {
        var list = LoadClassesFromFile();
        if (list.Any(c => c.ClassName == className)) return;
        list.Add(new ClassInfo { ClassName = className, Students = new List<string>() });
        SaveClassesToFile(list);
    }

    public void DeleteClass(string className)
    {
        var list = LoadClassesFromFile();
        list.RemoveAll(c => c.ClassName == className);
        SaveClassesToFile(list);

        var tlist = LoadTeachersFromFile();
        tlist.RemoveAll(t => t.ClassName == className);
        SaveTeachersToFile(tlist);
    }

    public void RenameClass(string oldName, string newName)
    {
        var list = LoadClassesFromFile();
        var cls = list.FirstOrDefault(c => c.ClassName == oldName);
        if (cls != null)
        {
            cls.ClassName = newName;
            SaveClassesToFile(list);

            var tlist = LoadTeachersFromFile();
            var t = tlist.FirstOrDefault(x => x.ClassName == oldName);
            if (t != null) { t.ClassName = newName; SaveTeachersToFile(tlist); }
        }
    }

    public void AddStudentToClass(string className, string studentName)
    {
        var list = LoadClassesFromFile();
        var cls = list.FirstOrDefault(c => c.ClassName == className);
        if (cls == null) return;
        if (!cls.Students.Contains(studentName)) cls.Students.Add(studentName);
        SaveClassesToFile(list);
    }

    public void UpdateStudentInClass(string className, string oldName, string newName)
    {
        var list = LoadClassesFromFile();
        var cls = list.FirstOrDefault(c => c.ClassName == className);
        if (cls == null) return;
        var idx = cls.Students.IndexOf(oldName);
        if (idx >= 0) cls.Students[idx] = newName;
        SaveClassesToFile(list);
    }

    public void DeleteStudentFromClass(string className, string studentName)
    {
        var list = LoadClassesFromFile();
        var cls = list.FirstOrDefault(c => c.ClassName == className);
        if (cls == null) return;
        cls.Students.RemoveAll(s => s == studentName);
        SaveClassesToFile(list);
    }

    // --- Teachers file ---
    private List<ClassTeacherInfo> LoadTeachersFromFile()
    {
        try
        {
            return File.Exists(classTeachersPath)
                ? JsonSerializer.Deserialize<List<ClassTeacherInfo>>(File.ReadAllText(classTeachersPath)) ?? new List<ClassTeacherInfo>()
                : new List<ClassTeacherInfo>();
        }
        catch
        {
            return new List<ClassTeacherInfo>();
        }
    }
    private void SaveTeachersToFile(List<ClassTeacherInfo> list)
    {
        File.WriteAllText(classTeachersPath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
    }

    public ClassTeacherInfo GetTeacherInfo(string className)
    {
        var list = LoadTeachersFromFile();
        var info = list.FirstOrDefault(c => c.ClassName == className);
        if (info == null)
        {
            info = new ClassTeacherInfo { ClassName = className, Teacher = "", CoTeacher = "", Description = "" };
            list.Add(info);
            SaveTeachersToFile(list);
        }
        return new ClassTeacherInfo
        {
            ClassName = info.ClassName,
            Teacher = info.Teacher,
            CoTeacher = info.CoTeacher,
            Description = info.Description
        };
    }

    public void SaveTeacherInfo(ClassTeacherInfo info)
    {
        var list = LoadTeachersFromFile();
        var existing = list.FirstOrDefault(c => c.ClassName == info.ClassName);
        if (existing != null)
        {
            existing.Teacher = info.Teacher;
            existing.CoTeacher = info.CoTeacher;
            existing.Description = info.Description;
        }
        else
        {
            list.Add(info);
        }
        SaveTeachersToFile(list);
    }
}
