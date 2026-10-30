using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerGuidancePortal.Data;
using CareerGuidancePortal.Models;
using CareerGuidancePortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5050");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));
builder.Services.AddHttpClient();builder.Services.AddScoped<AiRecommendationService>();builder.Services.AddEndpointsApiExplorer();builder.Services.AddSwaggerGen();
var jwtKey=builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer=builder.Configuration["Jwt:Issuer"],ValidAudience=builder.Configuration["Jwt:Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))});
builder.Services.AddAuthorization(o=>o.AddPolicy("AdminOnly",p=>p.RequireClaim("is_admin","true")));
var app=builder.Build();
using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();db.Database.EnsureCreated();if(!db.Admins.Any()){db.Admins.Add(new Admin{Name="Administrator",Email="admin@careerpath.com",PasswordHash=BCrypt.Net.BCrypt.HashPassword("Admin@123")});db.SaveChanges();}}
app.UseSwagger();app.UseSwaggerUI();app.UseAuthentication();app.UseAuthorization();
var frontendPath=Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,"..","Frontend"));
app.UseDefaultFiles(new DefaultFilesOptions{FileProvider=new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)});
app.UseStaticFiles(new StaticFileOptions{FileProvider=new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)});
app.MapGet("/api/health",()=>Results.Ok(new{status="ok",service="Career Guidance Portal",time=DateTime.UtcNow}));
app.MapPost("/api/auth/register",async(RegisterRequest req,AppDbContext db,IConfiguration cfg)=>{if(string.IsNullOrWhiteSpace(req.Name)||string.IsNullOrWhiteSpace(req.Email)||req.Password.Length<6)return Results.BadRequest(new{message="Name, email and a password of at least 6 characters are required."});var email=req.Email.Trim().ToLowerInvariant();if(await db.Students.AnyAsync(x=>x.Email==email))return Results.Conflict(new{message="An account with this email already exists."});var s=new Student{Name=req.Name.Trim(),Email=email,PasswordHash=BCrypt.Net.BCrypt.HashPassword(req.Password)};db.Students.Add(s);await db.SaveChangesAsync();db.Notifications.Add(new Notification{StudentId=s.Id,Title="Welcome to CareerPath AI",Message="Complete your profile to unlock personalized career recommendations."});await db.SaveChangesAsync();return Results.Ok(new{token=Token(s,cfg)});});
app.MapPost("/api/auth/login",async(LoginRequest req,AppDbContext db,IConfiguration cfg)=>{var s=await db.Students.FirstOrDefaultAsync(x=>x.Email==req.Email.Trim().ToLowerInvariant());if(s is null||!BCrypt.Net.BCrypt.Verify(req.Password,s.PasswordHash))return Results.Unauthorized();return Results.Ok(new{token=Token(s,cfg)});});
app.MapGet("/api/profile",async(ClaimsPrincipal user,AppDbContext db)=>{var s=await db.Students.FindAsync(UserId(user));return s is null?Results.NotFound():Results.Ok(Profile(s));}).RequireAuthorization();
app.MapPut("/api/profile",async(ProfileRequest r,ClaimsPrincipal user,AppDbContext db)=>{var s=await db.Students.FindAsync(UserId(user));if(s is null)return Results.NotFound();s.Name=r.Name?.Trim()??s.Name;s.Education=r.Education?.Trim()??"";s.College=r.College?.Trim()??"";s.Skills=r.Skills?.Trim()??"";s.Interests=r.Interests?.Trim()??"";s.PreferredLocation=r.PreferredLocation?.Trim()??"";s.CareerGoal=r.CareerGoal?.Trim()??"";await db.SaveChangesAsync();return Results.Ok(Profile(s));}).RequireAuthorization();
app.MapGet("/api/recommendations",async(ClaimsPrincipal user,AppDbContext db,AiRecommendationService ai)=>{var s=await db.Students.FindAsync(UserId(user));if(s is null)return Results.NotFound();return Results.Ok(new{careers=await ai.GetRecommendations(s)});}).RequireAuthorization();
app.MapGet("/api/courses",async(AppDbContext db)=>Results.Ok(await db.Courses.AsNoTracking().ToListAsync()));
app.MapGet("/api/colleges",async(AppDbContext db)=>Results.Ok((await db.Colleges.AsNoTracking().ToListAsync()).Select(c=>new{c.Id,c.Name,c.City,c.Type,Programs=c.Programs.Split(',',StringSplitOptions.TrimEntries),c.Description})));
app.MapGet("/api/mentors",async(AppDbContext db)=>Results.Ok((await db.Mentors.Where(x=>x.Available).AsNoTracking().ToListAsync()).Select(m=>new{m.Id,m.Name,m.Role,m.Company,Expertise=m.Expertise.Split(',',StringSplitOptions.TrimEntries),m.Bio}))).RequireAuthorization();
app.MapPost("/api/mentors/{id:int}/request",async(int id,ClaimsPrincipal user,AppDbContext db)=>{var sid=UserId(user);if(!await db.Mentors.AnyAsync(x=>x.Id==id&&x.Available))return Results.NotFound();if(await db.MentorRequests.AnyAsync(x=>x.StudentId==sid&&x.MentorId==id&&x.Status=="Pending"))return Results.Conflict(new{message="You already have a pending request for this mentor."});db.MentorRequests.Add(new MentorRequest{StudentId=sid,MentorId=id});db.Notifications.Add(new Notification{StudentId=sid,Title="Mentor request sent",Message="Your mentor session request has been recorded."});await db.SaveChangesAsync();return Results.Ok(new{message="Request sent."});}).RequireAuthorization();
app.MapGet("/api/notifications",async(ClaimsPrincipal user,AppDbContext db)=>{var sid=UserId(user);var notifications=await db.Notifications.Where(x=>x.StudentId==sid).OrderByDescending(x=>x.CreatedAt).AsNoTracking().ToListAsync();return Results.Ok(notifications);}).RequireAuthorization();
app.MapAdminEndpoints();app.MapFallback(async context=>{context.Response.ContentType="text/html";await context.Response.SendFileAsync(Path.Combine(frontendPath,"index.html"));});
app.Run();
static int UserId(ClaimsPrincipal u)=>int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
static object Profile(Student s)=>new{s.Id,s.Name,s.Email,s.Education,s.College,s.Skills,s.Interests,s.PreferredLocation,s.CareerGoal};
static string Token(Student s,IConfiguration cfg){var claims=new[]{new Claim(ClaimTypes.NameIdentifier,s.Id.ToString()),new Claim(ClaimTypes.Email,s.Email),new Claim(ClaimTypes.Name,s.Name)};var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));var t=new JwtSecurityToken(cfg["Jwt:Issuer"],cfg["Jwt:Audience"],claims,expires:DateTime.UtcNow.AddDays(7),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256));return new JwtSecurityTokenHandler().WriteToken(t);}
record RegisterRequest(string Name,string Email,string Password);record LoginRequest(string Email,string Password);record ProfileRequest(string? Name,string? Education,string? College,string? Skills,string? Interests,string? PreferredLocation,string? CareerGoal);
