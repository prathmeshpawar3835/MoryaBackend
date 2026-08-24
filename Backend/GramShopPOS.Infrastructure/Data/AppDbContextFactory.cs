using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GramShopPOS.Infrastructure.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var current = Directory.GetCurrentDirectory();
        var apiPath = Path.GetFullPath(Path.Combine(current, "../GramShopPOS.API"));
        if (!File.Exists(Path.Combine(apiPath, "appsettings.json")))
        {
            apiPath = current;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>();
        var cs = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=GramShopPOS;Trusted_Connection=True;TrustServerCertificate=True;";
        options.UseSqlServer(cs);
        return new AppDbContext(options.Options);
    }
}
