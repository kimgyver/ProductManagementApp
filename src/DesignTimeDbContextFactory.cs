using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
  public ApplicationDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

    // Load configuration (appsettings.json) for the connection string
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();

    // Configure DbContext with SQLite connection string
    optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));

    return new ApplicationDbContext(optionsBuilder.Options);
  }
}
