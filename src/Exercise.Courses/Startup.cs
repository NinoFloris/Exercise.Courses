using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Exercise.Courses.Features.Courses;
using Exercise.Courses.Models;
using Exercise.Courses.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Exercise.Courses
{
    public class Startup
    {
        public WebHostBuilderContext Context { get; }

        public Startup(WebHostBuilderContext context)
        {
            Context = context;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = Context.Configuration;
            services.AddDbContextPool<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("postgres")));

            services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
            services.AddSingleton<IdentityManager>();
            services.AddSingleton<PersonManager>();
            services.AddSingleton<CourseManager>();

            services.AddHostedService<StatisticsBackgroundService>();
            services.Configure<StatisticsBackgroundServiceOptions>(o =>
            {
                o.CheckInterval = TimeSpan.FromMinutes(30);
            });

            services.AddSingleton<EventQueue<Signup.Command>>();
            services.AddSingleton<IEventQueue<Signup.Command>>(sp => sp.GetRequiredService<EventQueue<Signup.Command>>());
            services.AddHostedService<EventQueueService<Signup.Command>>();

            // Normally MediatR or some utility library would handle this.
            services.AddScoped<IEventHandler<Signup.Command>, Signup.Handler>();
            services.AddScoped<IRequestHandler<Signup.Command, Result<Person, ValidationError>>, Signup.Handler>();
            services.AddScoped<IRequestHandler<Overview.Query, List<Overview.Model>>, Overview.Handler>();
            services.AddScoped<IRequestHandler<Details.Query, Result<Details.Model, ValidationError>>, Details.Handler>();

            services.AddScoped<StatisticsService>();
            services.AddScoped<MailingService>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Normally you'd put this in an extension class
                app.UseExceptionHandler(a =>
                    a.Use(async (ctx, next) =>
                    {
                        if (!ctx.Response.HasStarted)
                        {
                            var content = JsonConvert.SerializeObject(Web.UnknownServerProblem());
                            await ctx.Response.Body.WriteAsync(System.Text.Encoding.Unicode.GetBytes(content));
                        }
                    })
                );
            }

            app.UseMvc();
        }
    }
}
