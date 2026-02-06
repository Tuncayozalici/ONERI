using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ONERI.Data
{
    public class FabrikaContextFactory : IDesignTimeDbContextFactory<FabrikaContext>
    {
        public FabrikaContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=fabrika.db";
            var optionsBuilder = new DbContextOptionsBuilder<FabrikaContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new FabrikaContext(optionsBuilder.Options);
        }
    }
}
