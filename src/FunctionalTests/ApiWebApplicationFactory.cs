using System;
using System.IO;
using System.Linq;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FunctionalTests
{
    public class ApiWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var pathToWeb = SolutionPathUtility.GetProjectPath(@"PaymentGateway");
            var pathToApiStub = SolutionPathUtility.GetProjectPath(@"BankApi\ApiStub");

            builder
                .UseSolutionRelativeContentRoot(AppContext.BaseDirectory.Replace($".{Constants.Environment}", ""))
                .ConfigureAppConfiguration((context, conf) =>
                {
                    conf.AddJsonFile(Path.Combine(pathToWeb,
                        $"{Constants.AppsettingsFileName}.{Constants.AppsettingsFileExtension}"));
                    conf.AddJsonFile(Path.Combine(pathToWeb,
                        $"{Constants.AppsettingsFileName}.{Constants.Environment}.{Constants.AppsettingsFileExtension}"));

                    conf.AddJsonFile(Path.Combine(pathToApiStub,
                        $"{Constants.AppsettingsFileName}.{Constants.AppsettingsFileExtension}"));
                    conf.AddJsonFile(Path.Combine(pathToApiStub,
                        $"{Constants.AppsettingsFileName}.{Constants.Environment}.{Constants.AppsettingsFileExtension}"));
                })
                .ConfigureServices(services =>
                {
                    var descriptor =
                        services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(new Guid().ToString());
                    });

                    var sp = services.BuildServiceProvider();

                    using (var scope = sp.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<AppDbContext>();

                        var logger = scopedServices
                            .GetRequiredService<ILogger<ApiWebApplicationFactory<TStartup>>>();

                        // Ensure the database is created.
                        db.Database.EnsureCreated();

                        try
                        {
                            // Seed the database with test data.
                            TestDataHelper.PopulateTestData(db);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "An error occurred seeding the " +
                                                $"database with test messages. Error: {ex.Message}");
                        }
                    }
                });

            base.ConfigureWebHost(builder);
        }
    }
}