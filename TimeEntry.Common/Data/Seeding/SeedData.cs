using TimeEntry.Common.Data.Entities;

namespace TimeEntry.Common.Data.Seeding;

public static class SeedData
{
    public static IEnumerable<Project> GetProjects()
    {
        return new[]
        {
              new Project()
              {
                ProjectId = 1,
                Name = "Website Development",
                IsDefault = true,
                IsActive = true
              },
              new Project()
              {
                ProjectId = 2,
                Name = "Mobile App Development",
                IsDefault = true,
                IsActive = true
              },
              new Project()
              {
                ProjectId = 3,
                Name = "E-commerce Platform",
                IsDefault = true,
                IsActive = true
              },
              new Project()
              {
                ProjectId = 4,
                Name = "CRM System",
                IsDefault = true,
                IsActive = true
              },
              new Project()
              {
                ProjectId = 5,
                Name = "AI Chatbot Development",
                IsDefault = true,
                IsActive = true
              },
              new Project()
              {
                ProjectId = 6,
                Name = "Data Analytics Platform",
                IsDefault = true,
                IsActive = true
              },
            };
    }
}
