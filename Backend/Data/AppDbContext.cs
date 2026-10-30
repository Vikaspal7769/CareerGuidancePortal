using CareerGuidancePortal.Models;
using Microsoft.EntityFrameworkCore;
namespace CareerGuidancePortal.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>(); public DbSet<Mentor> Mentors => Set<Mentor>(); public DbSet<Course> Courses => Set<Course>(); public DbSet<College> Colleges => Set<College>(); public DbSet<MentorRequest> MentorRequests => Set<MentorRequest>(); public DbSet<Notification> Notifications => Set<Notification>(); public DbSet<Admin> Admins => Set<Admin>(); public DbSet<CareerCategory> CareerCategories => Set<CareerCategory>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Student>().HasIndex(x=>x.Email).IsUnique();
        b.Entity<Admin>().HasIndex(x=>x.Email).IsUnique();
        b.Entity<CareerCategory>().HasData(
            new CareerCategory{Id=1,Name="Software Development",Description="Web, mobile and enterprise software careers."},
            new CareerCategory{Id=2,Name="Data Science & AI",Description="Analytics, machine learning and AI careers."},
            new CareerCategory{Id=3,Name="Cyber Security",Description="Protecting systems, networks and data."},
            new CareerCategory{Id=4,Name="Cloud & DevOps",Description="Cloud platforms, automation and infrastructure."},
            new CareerCategory{Id=5,Name="UI/UX Design",Description="User interface and experience design careers."});
        b.Entity<Student>().HasMany(x=>x.Notifications).WithOne().HasForeignKey(x=>x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Mentor>().HasData(
            new Mentor{Id=1,Name="Aarav Mehta",Role="Senior Software Engineer",Company="Product Technology",Expertise="Backend Development,Cloud,Java",Bio="Helps students understand software engineering careers and interview preparation."},
            new Mentor{Id=2,Name="Neha Shah",Role="Data Scientist",Company="Analytics Studio",Expertise="Python,AI,Machine Learning",Bio="Guides students building a foundation in AI and data science."},
            new Mentor{Id=3,Name="Rohan Kulkarni",Role="Cybersecurity Analyst",Company="Security Operations",Expertise="Cyber Security,Networking,Ethical Hacking",Bio="Supports learners exploring cybersecurity roles and practical skills."});
        b.Entity<Course>().HasData(
            new Course{Id=1,Name="Python for Everybody",Provider="Coursera",Duration="8 weeks",Level="Beginner",Mode="Online",Description="Python fundamentals, data structures and practical programming.",Url="https://www.coursera.org/"},
            new Course{Id=2,Name="Machine Learning Specialization",Provider="Coursera",Duration="3 months",Level="Intermediate",Mode="Online",Description="Core supervised and unsupervised machine learning concepts.",Url="https://www.coursera.org/"},
            new Course{Id=3,Name="Full Stack Web Development",Provider="freeCodeCamp",Duration="Self paced",Level="Beginner",Mode="Online",Description="HTML, CSS, JavaScript and full-stack development foundations.",Url="https://www.freecodecamp.org/"},
            new Course{Id=4,Name="Cybersecurity Fundamentals",Provider="Cisco Networking Academy",Duration="Self paced",Level="Beginner",Mode="Online",Description="Security principles, threats, networking and defensive practices.",Url="https://www.netacad.com/"},
            new Course{Id=5,Name="SQL for Data Analysis",Provider="Khan Academy",Duration="Self paced",Level="Beginner",Mode="Online",Description="Query databases and work with structured data.",Url="https://www.khanacademy.org/"});
        b.Entity<College>().HasData(
            new College{Id=1,Name="VIVA College",City="Virar, Maharashtra",Type="Degree College",Programs="Computer Science,IT,Data Science",Description="Programs suitable for students building technology and computing foundations."},
            new College{Id=2,Name="University of Mumbai",City="Mumbai, Maharashtra",Type="University",Programs="Computer Science,Engineering,Management",Description="Wide range of undergraduate and postgraduate pathways."},
            new College{Id=3,Name="IIT Bombay",City="Mumbai, Maharashtra",Type="Institute",Programs="Computer Science,AI,Engineering",Description="Technology-focused academic and research programs."},
            new College{Id=4,Name="NMIMS",City="Mumbai, Maharashtra",Type="University",Programs="Technology,Management,Data Science",Description="Technology and management-oriented programs."});
    }
}
