using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamSystem.Data;

namespace ExamSystem.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ExamDbContext>
    {
        public ExamDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ExamDbContext>();
            optionsBuilder.UseSqlite("Data Source=exam_system.db");

            return new ExamDbContext(optionsBuilder.Options);
        }
    }
}