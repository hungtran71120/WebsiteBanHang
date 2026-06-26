using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HungStore.Infrastructure.Persistence;

namespace HungStore.API.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Server=.;Database=ShopeeCloneDb_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(TestConnectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        EnsureFreshTestDatabase();

        return base.CreateHost(builder);
    }

    private static void EnsureFreshTestDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(TestConnectionString).Options;
        using var context = new AppDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }
}
