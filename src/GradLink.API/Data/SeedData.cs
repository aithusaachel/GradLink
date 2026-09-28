using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Services;
using GradLink.Shared.Enums;

namespace GradLink.API.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<GradLinkDbContext>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync()) return;

        await using var transaction = await context.Database.BeginTransactionAsync();

        var employer1 = new ApplicationUser
        {
            UserName = "techcorp@gradlink.com",
            Email = "techcorp@gradlink.com",
            FullName = "TechCorp HR",
            Role = UserRole.Employer,
            CompanyName = "TechCorp Solutions",
            Industry = "Technology",
            CompanyDescription = "A leading technology company specialising in cloud solutions and enterprise software development. We empower businesses through innovative digital transformation strategies.",
            Website = "https://techcorp.example.com",
            EmailConfirmed = true
        };

        var employer2 = new ApplicationUser
        {
            UserName = "greenfinance@gradlink.com",
            Email = "greenfinance@gradlink.com",
            FullName = "Green Finance Team",
            Role = UserRole.Employer,
            CompanyName = "Green Finance Ltd",
            Industry = "Finance",
            CompanyDescription = "Sustainable finance company focused on green investments and ESG-compliant financial products for the modern investor.",
            Website = "https://greenfinance.example.com",
            EmailConfirmed = true
        };

        var employer3 = new ApplicationUser
        {
            UserName = "healthplus@gradlink.com",
            Email = "healthplus@gradlink.com",
            FullName = "HealthPlus Recruiting",
            Role = UserRole.Employer,
            CompanyName = "HealthPlus Medical",
            Industry = "Healthcare",
            CompanyDescription = "Innovative healthcare technology provider improving patient outcomes through AI-powered diagnostics and telemedicine platforms.",
            Website = "https://healthplus.example.com",
            EmailConfirmed = true
        };

        await CreateUserAsync(userManager, employer1, "Password@123");
        await CreateUserAsync(userManager, employer2, "Password@123");
        await CreateUserAsync(userManager, employer3, "Password@123");

        // Create sample graduates
        var graduateAlice = new ApplicationUser
        {
            UserName = "alice@gradlink.com",
            Email = "alice@gradlink.com",
            FullName = "Alice Johnson",
            Role = UserRole.Graduate,
            University = "University of Ghana",
            Degree = "BSc Computer Science",
            GraduationYear = 2025,
            Skills = "C#, Blazor, ASP.NET Core, SQL, Git",
            Bio = "Enthusiastic software engineer specializing in web application development.",
            EmailConfirmed = true
        };

        var graduate1 = new ApplicationUser
        {
            UserName = "john.doe@gradlink.com",
            Email = "john.doe@gradlink.com",
            FullName = "John Doe",
            Role = UserRole.Graduate,
            University = "University of Ghana",
            Degree = "BSc Computer Science",
            GraduationYear = 2025,
            Skills = "C#, Python, JavaScript, React, SQL, Git",
            Bio = "Passionate software developer with experience in web development and data analysis. Looking for entry-level software engineering roles.",
            EmailConfirmed = true
        };

        var graduate2 = new ApplicationUser
        {
            UserName = "jane.smith@gradlink.com",
            Email = "jane.smith@gradlink.com",
            FullName = "Jane Smith",
            Role = UserRole.Graduate,
            University = "Kwame Nkrumah University of Science and Technology",
            Degree = "BSc Information Technology",
            GraduationYear = 2025,
            Skills = "Java, Spring Boot, MySQL, Docker, AWS",
            Bio = "Detail-oriented IT graduate interested in backend development and cloud infrastructure.",
            EmailConfirmed = true
        };

        await CreateUserAsync(userManager, graduateAlice, "Password@123");
        await CreateUserAsync(userManager, graduate1, "Password@123");
        await CreateUserAsync(userManager, graduate2, "Password@123");

        // Create sample job listings
        var jobs = new List<JobListing>
        {
            new()
            {
                Title = "Junior Software Developer",
                Description = "We are looking for a motivated Junior Software Developer to join our growing engineering team. You will work on building and maintaining web applications using modern frameworks. Responsibilities include writing clean code, participating in code reviews, and collaborating with senior developers on feature design.\n\nRequirements:\n• BSc in Computer Science or related field\n• Familiarity with C#, JavaScript, or Python\n• Understanding of RESTful APIs and databases\n• Strong problem-solving skills",
                Location = "Accra, Ghana",
                Industry = "Technology",
                ExperienceLevel = ExperienceLevel.Entry,
                SalaryRange = "GHS 3,000 - 5,000",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                Deadline = DateTime.UtcNow.AddDays(25),
                EmployerId = employer1.Id
            },
            new()
            {
                Title = "Frontend Developer Intern",
                Description = "Join TechCorp as a Frontend Developer Intern and gain hands-on experience building responsive user interfaces with React and TypeScript. You'll work alongside experienced engineers on real-world products used by thousands of users.\n\nRequirements:\n• Currently pursuing or recently completed a degree in CS/IT\n• Knowledge of HTML, CSS, JavaScript\n• Experience with React is a plus\n• Eagerness to learn and grow",
                Location = "Accra, Ghana",
                Industry = "Technology",
                ExperienceLevel = ExperienceLevel.Entry,
                SalaryRange = "GHS 1,500 - 2,500",
                PostedDate = DateTime.UtcNow.AddDays(-3),
                Deadline = DateTime.UtcNow.AddDays(27),
                EmployerId = employer1.Id
            },
            new()
            {
                Title = "Graduate Financial Analyst",
                Description = "Green Finance Ltd is seeking a bright and analytical Graduate Financial Analyst to join our investment research team. You will assist in analysing market trends, preparing financial models, and supporting senior analysts in evaluating ESG-compliant investment opportunities.\n\nRequirements:\n• BSc in Finance, Economics, or Accounting\n• Proficiency in Excel and financial modelling\n• Strong analytical and quantitative skills\n• Interest in sustainable finance",
                Location = "Kumasi, Ghana",
                Industry = "Finance",
                ExperienceLevel = ExperienceLevel.Entry,
                SalaryRange = "GHS 3,500 - 5,500",
                PostedDate = DateTime.UtcNow.AddDays(-7),
                Deadline = DateTime.UtcNow.AddDays(23),
                EmployerId = employer2.Id
            },
            new()
            {
                Title = "Data Entry Clerk",
                Description = "We need a meticulous Data Entry Clerk to help manage our financial records and client databases. This role is ideal for fresh graduates who are detail-oriented and comfortable working with data.\n\nRequirements:\n• Diploma or Degree in any field\n• Fast and accurate typing skills\n• Proficiency in Microsoft Office\n• Good organisational skills",
                Location = "Remote",
                Industry = "Finance",
                ExperienceLevel = ExperienceLevel.Entry,
                SalaryRange = "GHS 1,800 - 2,800",
                PostedDate = DateTime.UtcNow.AddDays(-1),
                Deadline = DateTime.UtcNow.AddDays(29),
                EmployerId = employer2.Id
            },
            new()
            {
                Title = "Junior Health Data Analyst",
                Description = "HealthPlus Medical is hiring a Junior Health Data Analyst to support our data science team. You will work on analysing patient data, generating reports, and helping improve our AI diagnostic tools through data preparation and quality assurance.\n\nRequirements:\n• BSc in Statistics, Data Science, Public Health, or related field\n• Experience with Python, R, or SQL\n• Understanding of data visualisation tools\n• Interest in healthcare technology",
                Location = "Accra, Ghana",
                Industry = "Healthcare",
                ExperienceLevel = ExperienceLevel.Entry,
                SalaryRange = "GHS 3,000 - 4,500",
                PostedDate = DateTime.UtcNow.AddDays(-2),
                Deadline = DateTime.UtcNow.AddDays(28),
                EmployerId = employer3.Id
            },
            new()
            {
                Title = "IT Support Technician",
                Description = "HealthPlus is looking for an IT Support Technician to maintain our hospital network infrastructure, troubleshoot hardware and software issues, and provide technical support to medical staff.\n\nRequirements:\n• Degree or diploma in IT or Computer Engineering\n• Knowledge of networking fundamentals\n• Experience with Windows and Linux systems\n• Good communication skills",
                Location = "Takoradi, Ghana",
                Industry = "Healthcare",
                ExperienceLevel = ExperienceLevel.Entry,
                SalaryRange = "GHS 2,500 - 3,500",
                PostedDate = DateTime.UtcNow.AddDays(-4),
                Deadline = DateTime.UtcNow.AddDays(26),
                EmployerId = employer3.Id
            }
        };

        context.JobListings.AddRange(jobs);
        await context.SaveChangesAsync();

        // Create sample applications
        var applications = new List<JobApplication>
        {
            new()
            {
                JobListingId = jobs[0].Id,
                GraduateId = graduate1.Id,
                Status = ApplicationStatus.Reviewed,
                AppliedDate = DateTime.UtcNow.AddDays(-4),
                CoverLetter = "I am very excited about this Junior Software Developer position. With my background in Computer Science and hands-on experience with C# and web development, I believe I would be a great fit for your team."
            },
            new()
            {
                JobListingId = jobs[4].Id,
                GraduateId = graduate1.Id,
                Status = ApplicationStatus.Pending,
                AppliedDate = DateTime.UtcNow.AddDays(-1),
                CoverLetter = "I am interested in the Health Data Analyst role. My skills in Python and SQL, combined with my passion for technology in healthcare, make me a strong candidate."
            }
        };

        context.JobApplications.AddRange(applications);

        // The history the sample applications would have produced
        context.Notifications.AddRange(
            new Notification
            {
                UserId = employer1.Id,
                Message = NotificationMessages.NewApplication(graduate1.FullName, jobs[0].Title),
                Type = NotificationType.ApplicationUpdate,
                CreatedAt = applications[0].AppliedDate,
                IsRead = true
            },
            new Notification
            {
                UserId = graduate1.Id,
                Message = NotificationMessages.StatusChanged(jobs[0].Title, applications[0].Status),
                Type = NotificationType.ApplicationUpdate,
                CreatedAt = applications[0].AppliedDate.AddDays(1)
            },
            new Notification
            {
                UserId = employer3.Id,
                Message = NotificationMessages.NewApplication(graduate1.FullName, jobs[4].Title),
                Type = NotificationType.ApplicationUpdate,
                CreatedAt = applications[1].AppliedDate
            });

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string password)
    {
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Seeding user '{user.Email}' failed: {errors}");
        }
    }
}
