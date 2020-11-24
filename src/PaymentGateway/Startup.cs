using System;
using Core.Handlers;
using Core.PipelineBehaviors;
using Core.Validations;
using FluentValidation;
using Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace PaymentGateway
{
    public class Startup
    {
        private readonly IWebHostEnvironment env;

        public Startup(IConfiguration config, IWebHostEnvironment env)
        {
            Configuration = config;
            this.env = env;
        }

        public static IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options => { options.UseInMemoryDatabase(new Guid().ToString()); });

            // uncomment below lines to use the SQL database connection
            //var connectionString = Configuration.GetConnectionString("DefaultConnection");
            //services.AddDbContext<AppDbContext>(optionsBuilder => { optionsBuilder.UseSqlServer(connectionString); });
            //services.AddCustomLogging(Configuration);

            services
                .AddMvc(opt => opt.EnableEndpointRouting = false);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "My API", Version = "v1"});
                c.EnableAnnotations();
            });

            services.AddHealthChecks().AddCheck<BankApiHealthCheck>(nameof(BankApiHealthCheck));

            services.AddMediatR(typeof(PaymentProcessHandler).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddValidatorsFromAssembly(typeof(PaymentProcessValidator).Assembly);

            services.AddBankClient(Configuration);
            services.AddInfrastructureServices(Configuration);
        }

        //public void ConfigureContainer(ContainerBuilder builder)
        //{
        //    //builder.RegisterModule(new DefaultInfrastructureModule(_env.EnvironmentName == "Development"));
        //}


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHealthChecks("/api/health");
            //app.UseHttpsRedirection();
            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Gateway API V1"));
        }
    }
}