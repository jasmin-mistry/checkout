using System;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace PaymentGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    //context.Database.Migrate();
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseSerilog(((context, configuration) =>
                        {
                            configuration
                                .ReadFrom.Configuration(context.Configuration)
                                .Enrich.FromLogContext()
                                .MinimumLevel.Information()
                                .WriteTo.Console()
                                .WriteTo.Debug()
                                .WriteTo.File(new RenderedCompactJsonFormatter(), "log-.json",
                                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, buffered: true);
                        }))
                        .UseStartup<Startup>();
                });
    }
}