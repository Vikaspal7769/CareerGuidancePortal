using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerGuidancePortal.Data;
using CareerGuidancePortal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        // Public: list of career categories
        app.MapGet("/api/categories", async (AppDbContext db) =>
            Results.Ok(await db.CareerCategories.AsNoTracking().ToListAsync()));

        // Admin login (returns a token that carries the is_admin claim)
        app.MapPost("/api/admin/login", async (AdminLogin req, AppDbContext db, IConfiguration cfg) =>
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var admin = await db.Admins.FirstOrDefaultAsync(x => x.Email == email);
            if (admin is null || !BCrypt.Net.BCrypt.Verify(req.Password ?? "", admin.PasswordHash))
                return Results.Json(new { message = "Invalid admin email or password." }, statusCode: 401);
            return Results.Ok(new { token = AdminToken(admin, cfg), name = admin.Name });
        });

        // Everything below needs an admin token
        var g = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly");

        Crud<Mentor>(g, "/mentors");
        Crud<Course>(g, "/courses");
        Crud<College>(g, "/colleges");
        Crud<CareerCategory>(g, "/categories");

        g.MapGet("/stats", async (AppDbContext db) =>
        {
            var start = DateTime.UtcNow.Date.AddDays(-6);
            var recent = await db.Students.Where(s => s.CreatedAt >= start).Select(s => s.CreatedAt).ToListAsync();
            return Results.Ok(new
            {
                students = await db.Students.CountAsync(),
                mentors = await db.Mentors.CountAsync(),
                courses = await db.Courses.CountAsync(),
                colleges = await db.Colleges.CountAsync(),
                categories = await db.CareerCategories.CountAsync(),
                pending = await db.MentorRequests.CountAsync(r => r.Status == "Pending"),
                approved = await db.MentorRequests.CountAsync(r => r.Status == "Approved"),
                rejected = await db.MentorRequests.CountAsync(r => r.Status == "Rejected"),
                last7days = Enumerable.Range(0, 7).Select(i => start.AddDays(i))
                    .Select(d => new { day = d.ToString("dd MMM"), count = recent.Count(c => c.Date == d) }).ToList()
            });
        });

        g.MapGet("/students", async (AppDbContext db) =>
            Results.Ok(await db.Students.AsNoTracking().OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.Id, s.Name, s.Email, s.Education, s.College, s.Skills, s.CareerGoal, s.CreatedAt }).ToListAsync()));

        g.MapDelete("/students/{id:int}", async (int id, AppDbContext db) =>
        {
            var s = await db.Students.FindAsync(id);
            if (s is null) return Results.NotFound(new { message = "Student not found." });
            db.MentorRequests.RemoveRange(db.MentorRequests.Where(r => r.StudentId == id));
            db.Students.Remove(s);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Student deleted." });
        });

        g.MapGet("/requests", async (AppDbContext db) =>
            Results.Ok(await (from r in db.MentorRequests
                              join s in db.Students on r.StudentId equals s.Id
                              join m in db.Mentors on r.MentorId equals m.Id
                              orderby r.CreatedAt descending
                              select new { r.Id, Student = s.Name, StudentEmail = s.Email, Mentor = m.Name, r.Status, r.CreatedAt }).ToListAsync()));

        g.MapPut("/requests/{id:int}", async (int id, StatusBody body, AppDbContext db) =>
        {
            var status = (body.Status ?? "").Trim();
            if (status != "Approved" && status != "Rejected")
                return Results.BadRequest(new { message = "Status must be Approved or Rejected." });
            var r = await db.MentorRequests.FindAsync(id);
            if (r is null) return Results.NotFound(new { message = "Request not found." });
            var mentor = await db.Mentors.FindAsync(r.MentorId);
            var who = mentor?.Name ?? "the mentor";
            r.Status = status;
            db.Notifications.Add(new Notification
            {
                StudentId = r.StudentId,
                Title = "Mentor request " + status.ToLower(),
                Message = status == "Approved"
                    ? "Your session request with " + who + " has been approved. The mentor will contact you soon."
                    : "Your session request with " + who + " was not approved this time. You can request another mentor."
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Request " + status.ToLower() + "." });
        });

        g.MapPost("/notifications", async (NotifyBody body, AppDbContext db) =>
        {
            var title = (body.Title ?? "").Trim();
            var message = (body.Message ?? "").Trim();
            if (title == "" || message == "")
                return Results.BadRequest(new { message = "Title and message are required." });
            List<int> ids;
            if (body.StudentId is int sid)
            {
                if (!await db.Students.AnyAsync(s => s.Id == sid))
                    return Results.NotFound(new { message = "Student not found." });
                ids = new List<int> { sid };
            }
            else ids = await db.Students.Select(s => s.Id).ToListAsync();
            db.Notifications.AddRange(ids.Select(i => new Notification { StudentId = i, Title = title, Message = message }));
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Notification sent to " + ids.Count + " student(s)." });
        });
    }

    // Generic list / add / edit / delete for one table
    static void Crud<T>(RouteGroupBuilder g, string path) where T : class
    {
        g.MapGet(path, async (AppDbContext db) => Results.Ok(await db.Set<T>().AsNoTracking().ToListAsync()));
        g.MapPost(path, async (T e, AppDbContext db) =>
        {
            db.Set<T>().Add(e);
            await db.SaveChangesAsync();
            return Results.Ok(e);
        });
        g.MapPut(path + "/{id:int}", async (int id, T e, AppDbContext db) =>
        {
            typeof(T).GetProperty("Id")!.SetValue(e, id);
            db.Set<T>().Update(e);
            await db.SaveChangesAsync();
            return Results.Ok(e);
        });
        g.MapDelete(path + "/{id:int}", async (int id, AppDbContext db) =>
        {
            var e = await db.Set<T>().FindAsync(id);
            if (e is null) return Results.NotFound(new { message = "Record not found." });
            db.Set<T>().Remove(e);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Deleted." });
        });
    }

    static string AdminToken(Admin a, IConfiguration cfg)
    {
        var claims = new[] { new Claim(ClaimTypes.Email, a.Email), new Claim(ClaimTypes.Name, a.Name), new Claim("is_admin", "true") };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var t = new JwtSecurityToken(cfg["Jwt:Issuer"], cfg["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(t);
    }
}

public record AdminLogin(string? Email, string? Password);
public record StatusBody(string? Status);
public record NotifyBody(int? StudentId, string? Title, string? Message);
