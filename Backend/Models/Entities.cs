namespace CareerGuidancePortal.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Education { get; set; } = "";
    public string College { get; set; } = "";
    public string Skills { get; set; } = "";
    public string Interests { get; set; } = "";
    public string PreferredLocation { get; set; } = "";
    public string CareerGoal { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Notification> Notifications { get; set; } = [];
}
public class Mentor { public int Id { get; set; } public string Name { get; set; }=""; public string Role { get; set; }=""; public string Company { get; set; }=""; public string Expertise { get; set; }=""; public string Bio { get; set; }=""; public bool Available { get; set; }=true; }
public class Course { public int Id { get; set; } public string Name { get; set; }=""; public string Provider { get; set; }=""; public string Duration { get; set; }=""; public string Level { get; set; }=""; public string Mode { get; set; }=""; public string Description { get; set; }=""; public string Url { get; set; }="#"; }
public class College { public int Id { get; set; } public string Name { get; set; }=""; public string City { get; set; }=""; public string Type { get; set; }=""; public string Programs { get; set; }=""; public string Description { get; set; }=""; }
public class MentorRequest { public int Id { get; set; } public int StudentId { get; set; } public int MentorId { get; set; } public string Status { get; set; }="Pending"; public DateTime CreatedAt { get; set; }=DateTime.UtcNow; }
public class Notification { public int Id { get; set; } public int StudentId { get; set; } public string Title { get; set; }=""; public string Message { get; set; }=""; public bool IsRead { get; set; } public DateTime CreatedAt { get; set; }=DateTime.UtcNow; }
public class CareerRecommendation { public string Title { get; set; }=""; public int MatchScore { get; set; } public string Description { get; set; }=""; public string Reason { get; set; }=""; public List<string> Skills { get; set; }=[]; }
public class Admin { public int Id { get; set; } public string Name { get; set; }=""; public string Email { get; set; }=""; public string PasswordHash { get; set; }=""; }
public class CareerCategory { public int Id { get; set; } public string Name { get; set; }=""; public string Description { get; set; }=""; }
